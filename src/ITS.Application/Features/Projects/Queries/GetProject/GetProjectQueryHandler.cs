using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Projects.Queries.GetProject;

public sealed class GetProjectQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<GetProjectQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        if (!await authz.CanViewProjectAsync(currentUser.UserId, request.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to view this project.");

        var project = await db.Projects.AsNoTracking()
            .Include(p => p.Members)
            .Include(p => p.IssueTypes)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var memberUserIds = project.Members.Select(m => m.UserId).ToList();
        var memberRoleIds = project.Members.Select(m => m.RoleId).Distinct().ToList();

        var usersTask = db.Users.AsNoTracking()
            .Where(u => memberUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.AvatarUrl })
            .ToListAsync(cancellationToken);

        var rolesTask = db.Roles.AsNoTracking()
            .Where(r => memberRoleIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(cancellationToken);

        var leadTask = db.Users.AsNoTracking()
            .Where(u => u.Id == project.LeadUserId)
            .Select(u => new { u.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);

        var deptTask = db.Departments.AsNoTracking()
            .Where(d => d.Id == project.DepartmentId)
            .Select(d => new { d.Name })
            .FirstOrDefaultAsync(cancellationToken);

        var workflowTask = project.ActiveWorkflowId.HasValue
            ? db.Workflows.AsNoTracking()
                .Where(w => w.Id == project.ActiveWorkflowId.Value)
                .Select(w => new { w.Name })
                .FirstOrDefaultAsync(cancellationToken)
            : Task.FromResult<object?>(null);

        var activeTicketCountTask = db.Tickets.AsNoTracking()
            .CountAsync(t => t.ProjectId == request.ProjectId && !t.IsDeleted, cancellationToken);

        await Task.WhenAll(usersTask, rolesTask, leadTask, deptTask, workflowTask, activeTicketCountTask);

        var users = (await usersTask).ToDictionary(u => u.Id);
        var roles = (await rolesTask).ToDictionary(r => r.Id);
        var lead = await leadTask;
        var dept = await deptTask;
        var activeTicketCount = await activeTicketCountTask;

        // Workflow name: re-query after WhenAll since workflowTask uses object?
        string? workflowName = null;
        if (project.ActiveWorkflowId.HasValue)
        {
            workflowName = await db.Workflows.AsNoTracking()
                .Where(w => w.Id == project.ActiveWorkflowId.Value)
                .Select(w => w.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var members = project.Members.Select(m => new ProjectMemberDto(
            m.Id,
            m.UserId,
            users.TryGetValue(m.UserId, out var u) ? u.DisplayName : "Unknown",
            users.TryGetValue(m.UserId, out var u2) ? u2.AvatarUrl : null,
            roles.TryGetValue(m.RoleId, out var r) ? r.Name : "Unknown",
            m.GrantedAt))
            .ToList();

        return new ProjectDetailDto(
            project.Id,
            project.ProjectKey,
            project.Name,
            project.Description,
            project.AvatarUrl,
            project.LeadUserId,
            lead?.DisplayName ?? "",
            project.DepartmentId,
            dept?.Name,
            project.ActiveWorkflowId,
            workflowName,
            project.IsArchived,
            project.ArchivedAt,
            project.CreatedAt,
            project.UpdatedAt,
            project.RowVersion,
            project.IssueTypes.Select(it => it.Name).ToList().AsReadOnly(),
            members.AsReadOnly(),
            project.Members.Count,
            activeTicketCount);
    }
}
