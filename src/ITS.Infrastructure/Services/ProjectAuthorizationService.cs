using ITS.Application.Common.Interfaces;
using ITS.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace ITS.Infrastructure.Services;

internal sealed class ProjectAuthorizationService(IApplicationDbContext db) : IProjectAuthorizationService
{
    private static readonly HashSet<string> ManagerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleNames.SystemAdministrator,
        RoleNames.ProjectLead
    };

    private static readonly HashSet<string> ContributorRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleNames.SystemAdministrator,
        RoleNames.ProjectLead,
        RoleNames.Developer
    };

    public async Task<bool> CanViewProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
    {
        if (await IsSystemAdminAsync(userId, cancellationToken))
            return true;

        return await db.ProjectMembers
            .AsNoTracking()
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken);
    }

    public async Task<bool> CanManageProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
    {
        if (await IsSystemAdminAsync(userId, cancellationToken))
            return true;

        var roleName = await GetProjectRoleNameAsync(userId, projectId, cancellationToken);
        return roleName is not null && ManagerRoles.Contains(roleName);
    }

    public async Task<bool> CanCreateTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
    {
        if (await IsSystemAdminAsync(userId, cancellationToken))
            return true;

        var roleName = await GetProjectRoleNameAsync(userId, projectId, cancellationToken);
        return roleName is not null && ContributorRoles.Contains(roleName);
    }

    public async Task<bool> CanEditTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => await CanCreateTicketAsync(userId, projectId, cancellationToken);

    public async Task<bool> CanDeleteTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => await CanManageProjectAsync(userId, projectId, cancellationToken);

    public async Task<bool> CanTransitionTicketAsync(Guid userId, Guid projectId, int requiredRoleId, CancellationToken cancellationToken = default)
    {
        if (await IsSystemAdminAsync(userId, cancellationToken))
            return true;

        if (requiredRoleId > 0)
        {
            return await db.ProjectMembers
                .AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == projectId
                             && pm.UserId == userId
                             && pm.RoleId == requiredRoleId,
                    cancellationToken);
        }

        return await CanCreateTicketAsync(userId, projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleProjectIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await IsSystemAdminAsync(userId, cancellationToken))
        {
            return await db.Projects
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        return await db.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> GetProjectRoleNameAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        return await (
            from pm in db.ProjectMembers.AsNoTracking()
            join r in db.Roles.AsNoTracking() on pm.RoleId equals r.Id
            where pm.ProjectId == projectId && pm.UserId == userId
            select r.Name
        ).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> IsSystemAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from ugr in db.UserGlobalRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ugr.RoleId equals r.Id
            where ugr.UserId == userId && r.Name == RoleNames.SystemAdministrator
            select ugr.Id
        ).AnyAsync(cancellationToken);
    }
}
