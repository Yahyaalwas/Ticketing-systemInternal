using FluentAssertions;
using ITS.Application.Common.Interfaces;
using ITS.Application.Features.Auth.Commands.Login;
using ITS.Domain.Entities.Identity;
using ITS.Domain.Enums;
using ITS.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITS.UnitTests.Application.Auth;

public class LoginCommandHandlerTests
{
    private static readonly AdUserInfo SampleAdUser = new(
        ObjectId: "ad-guid-001",
        UserPrincipalName: "jdoe@corp.example.com",
        Email: "jdoe@corp.example.com",
        DisplayName: "John Doe",
        FirstName: "John",
        LastName: "Doe",
        Department: "Engineering",
        Manager: null,
        JobTitle: "Engineer",
        PhoneNumber: "+1-555-0100",
        EmployeeId: "EMP-001",
        IsActive: true,
        GroupMemberships: []);

    private static LoginCommandHandler CreateHandler(
        Mock<IAdSyncService> adSync,
        Mock<IApplicationDbContext> db,
        Mock<ITokenService> tokenSvc)
    {
        return new LoginCommandHandler(
            adSync.Object, db.Object, tokenSvc.Object,
            NullLogger<LoginCommandHandler>.Instance);
    }

    private static Mock<IApplicationDbContext> BuildDbMock(
        List<User>? users = null,
        List<Domain.Entities.Identity.Department>? departments = null,
        List<AdGroupRoleMapping>? mappings = null)
    {
        users ??= [];
        departments ??= [];
        mappings ??= [];

        var db = new Mock<IApplicationDbContext>();
        db.Setup(d => d.Users).Returns(MockDbSetFactory.Create(users).Object);
        db.Setup(d => d.Departments).Returns(MockDbSetFactory.Create(departments).Object);
        db.Setup(d => d.AdGroupRoleMappings).Returns(MockDbSetFactory.Create(mappings).Object);
        db.Setup(d => d.UserGlobalRoles).Returns(MockDbSetFactory.Create(new List<UserGlobalRole>()).Object);
        db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return db;
    }

    [Fact]
    public async Task Handle_WhenAuthFails_ThrowsUnauthorizedAccessException()
    {
        var adSync = new Mock<IAdSyncService>();
        adSync.Setup(s => s.AuthenticateAsync("jdoe@corp.example.com", "wrong", default))
            .ReturnsAsync(false);

        var handler = CreateHandler(adSync, BuildDbMock(), new Mock<ITokenService>());

        var act = () => handler.Handle(new LoginCommand("jdoe@corp.example.com", "wrong"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid credentials*");
    }

    [Fact]
    public async Task Handle_WhenAdUserNotFound_ThrowsUnauthorizedAccessException()
    {
        var adSync = new Mock<IAdSyncService>();
        adSync.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        adSync.Setup(s => s.QueryUserAsync(It.IsAny<string>(), default)).ReturnsAsync((AdUserInfo?)null);

        var handler = CreateHandler(adSync, BuildDbMock(), new Mock<ITokenService>());

        var act = () => handler.Handle(new LoginCommand("ghost@corp.com", "pass"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_WhenAdUserDisabled_ThrowsUnauthorizedAccessException()
    {
        var disabledUser = SampleAdUser with { IsActive = false };
        var adSync = new Mock<IAdSyncService>();
        adSync.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        adSync.Setup(s => s.QueryUserAsync(It.IsAny<string>(), default)).ReturnsAsync(disabledUser);

        var handler = CreateHandler(adSync, BuildDbMock(), new Mock<ITokenService>());

        var act = () => handler.Handle(new LoginCommand(disabledUser.UserPrincipalName, "pass"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*disabled*");
    }

    [Fact]
    public async Task Handle_FirstLogin_ProvisionesNewUserAndReturnsToken()
    {
        var adSync = new Mock<IAdSyncService>();
        adSync.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        adSync.Setup(s => s.QueryUserAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleAdUser);

        var db = BuildDbMock(); // empty users list → new user scenario
        User? capturedUser = null;
        db.Setup(d => d.Users.Add(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u);

        var tokenSvc = new Mock<ITokenService>();
        tokenSvc.Setup(t => t.IssueToken(It.IsAny<TokenRequest>())).Returns("jwt-token");

        var handler = CreateHandler(adSync, db, tokenSvc);

        var result = await handler.Handle(new LoginCommand("jdoe@corp.example.com", "pass"), default);

        result.Token.Should().Be("jwt-token");
        result.DisplayName.Should().Be("John Doe");
        result.Email.Should().Be("jdoe@corp.example.com");
        capturedUser.Should().NotBeNull();
        capturedUser!.AdObjectId.Should().Be("ad-guid-001");
        capturedUser.EmployeeId.Should().Be("EMP-001");
        db.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SubsequentLogin_SyncsExistingUserFromAd()
    {
        var existingUser = User.ProvisionFromAd(
            "ad-guid-001", "jdoe@corp.example.com", "jdoe@corp.example.com",
            "Old Name", "Old", "Doe", null, "Junior Engineer", null, "EMP-OLD");

        var adSync = new Mock<IAdSyncService>();
        adSync.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        adSync.Setup(s => s.QueryUserAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleAdUser);

        var db = BuildDbMock(users: [existingUser]);
        var tokenSvc = new Mock<ITokenService>();
        tokenSvc.Setup(t => t.IssueToken(It.IsAny<TokenRequest>())).Returns("jwt-token");

        var handler = CreateHandler(adSync, db, tokenSvc);

        await handler.Handle(new LoginCommand("jdoe@corp.example.com", "pass"), default);

        // Verify the user was updated, not re-added
        db.Verify(d => d.Users.Add(It.IsAny<User>()), Times.Never);
        existingUser.DisplayName.Should().Be("John Doe");
        existingUser.EmployeeId.Should().Be("EMP-001");
        existingUser.JobTitle.Should().Be("Engineer");
    }

    [Fact]
    public async Task Handle_WithMatchingAdGroupMapping_GrantsRole()
    {
        var role = Role.Create("System Administrator", null, RoleScope.Global);
        // Set Id via reflection (int PK default is 0, force to 1)
        typeof(Role).GetProperty("Id")!.SetValue(role, 1);

        var mapping = AdGroupRoleMapping.Create(
            "CN=ITS-Admins,OU=Groups,DC=corp,DC=example,DC=com",
            "ITS-Admins", 1, null);
        typeof(AdGroupRoleMapping).GetProperty("Role")!.SetValue(mapping, role);

        var adUserWithGroup = SampleAdUser with
        {
            GroupMemberships = ["CN=ITS-Admins,OU=Groups,DC=corp,DC=example,DC=com"]
        };

        var adSync = new Mock<IAdSyncService>();
        adSync.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        adSync.Setup(s => s.QueryUserAsync(It.IsAny<string>(), default)).ReturnsAsync(adUserWithGroup);

        var db = BuildDbMock(mappings: [mapping]);
        UserGlobalRole? grantedRole = null;
        db.Setup(d => d.UserGlobalRoles.Add(It.IsAny<UserGlobalRole>()))
            .Callback<UserGlobalRole>(gr => grantedRole = gr);

        var tokenSvc = new Mock<ITokenService>();
        tokenSvc.Setup(t => t.IssueToken(It.IsAny<TokenRequest>())).Returns("jwt");

        var handler = CreateHandler(adSync, db, tokenSvc);
        var result = await handler.Handle(new LoginCommand("jdoe@corp.example.com", "pass"), default);

        grantedRole.Should().NotBeNull();
        grantedRole!.RoleId.Should().Be(1);
        result.Roles.Should().Contain("System Administrator");
    }

    [Fact]
    public async Task Handle_TokenRequest_ContainsCorrectClaims()
    {
        var adSync = new Mock<IAdSyncService>();
        adSync.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        adSync.Setup(s => s.QueryUserAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleAdUser);

        var db = BuildDbMock();
        TokenRequest? capturedRequest = null;
        var tokenSvc = new Mock<ITokenService>();
        tokenSvc.Setup(t => t.IssueToken(It.IsAny<TokenRequest>()))
            .Callback<TokenRequest>(r => capturedRequest = r)
            .Returns("jwt");

        var handler = CreateHandler(adSync, db, tokenSvc);
        await handler.Handle(new LoginCommand("jdoe@corp.example.com", "pass"), default);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserPrincipalName.Should().Be("jdoe@corp.example.com");
        capturedRequest.DisplayName.Should().Be("John Doe");
        capturedRequest.Email.Should().Be("jdoe@corp.example.com");
        capturedRequest.UserId.Should().NotBeEmpty();
    }
}
