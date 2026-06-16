using FluentAssertions;
using ITS.Domain.Entities.Identity;

namespace ITS.UnitTests.Domain;

public class UserProvisioningTests
{
    [Fact]
    public void ProvisionFromAd_WithRequiredFields_SetsAllProperties()
    {
        var adObjectId = Guid.NewGuid().ToString();

        var user = User.ProvisionFromAd(
            adObjectId,
            userPrincipalName: "jdoe@corp.example.com",
            email: "jdoe@corp.example.com",
            displayName: "John Doe",
            firstName: "John",
            lastName: "Doe",
            departmentId: 5,
            jobTitle: "Engineer",
            phoneNumber: "+1-555-0100",
            employeeId: "EMP-001");

        user.AdObjectId.Should().Be(adObjectId);
        user.UserPrincipalName.Should().Be("jdoe@corp.example.com");
        user.Email.Should().Be("jdoe@corp.example.com");
        user.DisplayName.Should().Be("John Doe");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.DepartmentId.Should().Be(5);
        user.JobTitle.Should().Be("Engineer");
        user.PhoneNumber.Should().Be("+1-555-0100");
        user.EmployeeId.Should().Be("EMP-001");
        user.IsActive.Should().BeTrue();
        user.TimeZoneId.Should().Be("UTC");
        user.Locale.Should().Be("en-US");
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ProvisionFromAd_NormalizesUPNAndEmailToLowercase()
    {
        var user = User.ProvisionFromAd(
            Guid.NewGuid().ToString(),
            "JDoe@Corp.Example.Com",
            "JDoe@Corp.Example.Com",
            "John Doe",
            null, null, null, null, null);

        user.UserPrincipalName.Should().Be("jdoe@corp.example.com");
        user.Email.Should().Be("jdoe@corp.example.com");
    }

    [Fact]
    public void ProvisionFromAd_WithoutEmployeeId_SetsItToNull()
    {
        var user = User.ProvisionFromAd(
            Guid.NewGuid().ToString(), "u@corp.com", "u@corp.com", "User", null, null, null, null, null);

        user.EmployeeId.Should().BeNull();
    }

    [Theory]
    [InlineData("", "valid@corp.com", "Display")]
    [InlineData("  ", "valid@corp.com", "Display")]
    public void ProvisionFromAd_EmptyAdObjectId_ThrowsArgumentException(
        string adObjectId, string email, string displayName)
    {
        var act = () => User.ProvisionFromAd(adObjectId, "upn@corp.com", email, displayName, null, null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SyncFromAd_UpdatesAllMutableFields()
    {
        var user = User.ProvisionFromAd(
            Guid.NewGuid().ToString(), "old@corp.com", "old@corp.com", "Old Name",
            "Old", "Name", null, null, null, employeeId: "EMP-OLD");

        var syncedAt = DateTime.UtcNow;
        user.SyncFromAd(
            "new@corp.com", "new@corp.com", "New Name",
            "New", "Name", departmentId: 7, managerUserId: null,
            jobTitle: "Senior Engineer", phoneNumber: "+1-555-0200",
            employeeId: "EMP-NEW", isActive: true, syncedAt);

        user.UserPrincipalName.Should().Be("new@corp.com");
        user.Email.Should().Be("new@corp.com");
        user.DisplayName.Should().Be("New Name");
        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("Name");
        user.DepartmentId.Should().Be(7);
        user.JobTitle.Should().Be("Senior Engineer");
        user.PhoneNumber.Should().Be("+1-555-0200");
        user.EmployeeId.Should().Be("EMP-NEW");
        user.LastAdSyncAt.Should().BeCloseTo(syncedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SyncFromAd_WhenDeactivated_SetsIsActiveFalse()
    {
        var user = User.ProvisionFromAd(
            Guid.NewGuid().ToString(), "u@corp.com", "u@corp.com", "User", null, null, null, null, null);

        user.SyncFromAd("u@corp.com", "u@corp.com", "User", null, null, null, null, null, null, null, isActive: false, DateTime.UtcNow);

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        var user = User.ProvisionFromAd(
            Guid.NewGuid().ToString(), "u@corp.com", "u@corp.com", "User", null, null, null, null, null);

        var loginTime = DateTime.UtcNow;
        user.RecordLogin(loginTime);

        user.LastLoginAt.Should().Be(loginTime);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = User.ProvisionFromAd(
            Guid.NewGuid().ToString(), "u@corp.com", "u@corp.com", "User", null, null, null, null, null);

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var user = User.ProvisionFromAd(
            Guid.NewGuid().ToString(), "u@corp.com", "u@corp.com", "User", null, null, null, null, null);
        user.Deactivate();

        user.Activate();

        user.IsActive.Should().BeTrue();
    }
}
