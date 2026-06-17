using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ITS.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IRequestHandler<UpdateProjectCommand>
{
    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (!await IsManagerAsync(request.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to update this project.");

        if (!project.RowVersion.SequenceEqual(request.RowVersion))
            throw new ConflictException("Project", request.ProjectId);

        _ = await db.Users.FindAsync([request.LeadUserId], cancellationToken)
            ?? throw new NotFoundException("User", request.LeadUserId);

        if (request.WorkflowId.HasValue)
            _ = await db.Workflows.FindAsync([request.WorkflowId.Value], cancellationToken)
                ?? throw new NotFoundException("Workflow", request.WorkflowId.Value);

        var before = JsonSerializer.Serialize(new { project.Name, project.Description, project.LeadUserId, project.AvatarUrl, project.ActiveWorkflowId });

        project.Update(request.Name, request.Description, request.LeadUserId, request.AvatarUrl);

        if (request.WorkflowId.HasValue)
            project.AssignWorkflow(request.WorkflowId.Value);

        await db.SaveChangesAsync(cancellationToken);

        var after = JsonSerializer.Serialize(new { request.Name, request.Description, request.LeadUserId, request.AvatarUrl, WorkflowId = request.WorkflowId });

        await auditService.LogAsync(AuditOperation.Update, "Project", project.Id.ToString(), project.Id, before, after, cancellationToken: cancellationToken);
    }

    private async Task<bool> IsManagerAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(RoleNames.SystemAdministrator)) return true;

        return await (
            from pm in db.ProjectMembers.AsNoTracking()
            join r in db.Roles.AsNoTracking() on pm.RoleId equals r.Id
            where pm.ProjectId == projectId && pm.UserId == currentUser.UserId
                  && (r.Name == RoleNames.ProjectLead || r.Name == RoleNames.SystemAdministrator)
            select pm.Id
        ).AnyAsync(cancellationToken);
    }
}
