using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Identity;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITS.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IAdSyncService adSyncService,
    IApplicationDbContext db,
    ITokenService tokenService,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var authenticated = await adSyncService.AuthenticateAsync(
            command.UserPrincipalName, command.Password, cancellationToken);

        if (!authenticated)
        {
            logger.LogWarning("Failed AD authentication for {UPN}", command.UserPrincipalName);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var adUser = await adSyncService.QueryUserAsync(command.UserPrincipalName, cancellationToken)
            ?? throw new UnauthorizedAccessException("User account not found in directory.");

        if (!adUser.IsActive)
            throw new UnauthorizedAccessException("User account is disabled in Active Directory.");

        // Resolve local department by AD department name
        int? departmentId = null;
        if (!string.IsNullOrWhiteSpace(adUser.Department))
        {
            var dept = await db.Departments
                .FirstOrDefaultAsync(d => d.Name == adUser.Department, cancellationToken);
            departmentId = dept?.Id;
        }

        // Load all global AD group → role mappings in one query
        var allGlobalMappings = await db.AdGroupRoleMappings
            .Include(m => m.Role)
            .Where(m => m.ProjectId == null)
            .ToListAsync(cancellationToken);

        var applicableMappings = adUser.GroupMemberships.Count > 0
            ? allGlobalMappings.Where(m => adUser.GroupMemberships.Contains(m.AdGroupDistinguishedName)).ToList()
            : [];

        var allManagedRoleIds = allGlobalMappings.Select(m => m.RoleId).ToHashSet();

        // Provision or sync local user record
        var user = await db.Users
            .Include(u => u.GlobalRoles).ThenInclude(gr => gr.Role)
            .FirstOrDefaultAsync(u => u.AdObjectId == adUser.ObjectId, cancellationToken);

        if (user is null)
        {
            user = User.ProvisionFromAd(
                adUser.ObjectId, adUser.UserPrincipalName, adUser.Email,
                adUser.DisplayName, adUser.FirstName, adUser.LastName,
                departmentId, adUser.JobTitle, adUser.PhoneNumber, adUser.EmployeeId);
            db.Users.Add(user);
            logger.LogInformation("Provisioned new user {UPN} from AD", adUser.UserPrincipalName);
        }
        else
        {
            user.SyncFromAd(
                adUser.UserPrincipalName, adUser.Email, adUser.DisplayName,
                adUser.FirstName, adUser.LastName, departmentId, null,
                adUser.JobTitle, adUser.PhoneNumber, adUser.EmployeeId,
                adUser.IsActive, DateTime.UtcNow);
        }

        user.RecordLogin(DateTime.UtcNow);

        // Sync roles derived from AD group memberships
        var roleNames = ApplyRoleMappings(user, applicableMappings, allManagedRoleIds);

        await db.SaveChangesAsync(cancellationToken);

        var token = tokenService.IssueToken(new TokenRequest(
            user.Id, user.UserPrincipalName, user.DisplayName, user.Email, roleNames));

        return new LoginResult(token, user.Id, user.DisplayName, user.Email, user.AvatarUrl, roleNames);
    }

    private IReadOnlyList<string> ApplyRoleMappings(
        User user,
        IReadOnlyList<AdGroupRoleMapping> applicableMappings,
        IReadOnlySet<int> allManagedRoleIds)
    {
        var currentRoleIds = user.GlobalRoles.Select(gr => gr.RoleId).ToHashSet();
        var entitledRoleIds = applicableMappings.Select(m => m.RoleId).ToHashSet();

        // Grant roles the user is now entitled to but doesn't yet have
        foreach (var mapping in applicableMappings.Where(m => !currentRoleIds.Contains(m.RoleId)))
        {
            db.UserGlobalRoles.Add(UserGlobalRole.Grant(user.Id, mapping.RoleId, SystemConstants.SystemUserId));
            logger.LogInformation("Auto-granted role '{Role}' to {UPN} via AD group",
                mapping.Role.Name, user.UserPrincipalName);
        }

        // Revoke AD-managed roles the user's groups no longer entitle them to
        foreach (var gr in user.GlobalRoles.Where(gr => allManagedRoleIds.Contains(gr.RoleId) && !entitledRoleIds.Contains(gr.RoleId)))
        {
            db.UserGlobalRoles.Remove(gr);
            logger.LogInformation("Revoked AD-managed role '{Role}' from {UPN}",
                gr.Role.Name, user.UserPrincipalName);
        }

        // Compute final role names for the JWT (from kept + newly granted roles)
        var revokedIds = user.GlobalRoles
            .Where(gr => allManagedRoleIds.Contains(gr.RoleId) && !entitledRoleIds.Contains(gr.RoleId))
            .Select(gr => gr.RoleId)
            .ToHashSet();

        var keptNames = user.GlobalRoles
            .Where(gr => !revokedIds.Contains(gr.RoleId))
            .Select(gr => gr.Role.Name);

        var grantedNames = applicableMappings
            .Where(m => !currentRoleIds.Contains(m.RoleId))
            .Select(m => m.Role.Name);

        return keptNames.Concat(grantedNames).Distinct().ToList().AsReadOnly();
    }
}
