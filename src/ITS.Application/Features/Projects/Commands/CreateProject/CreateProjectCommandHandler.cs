using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Config;
using ITS.Domain.Entities.Projects;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ITS.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IRequestHandler<CreateProjectCommand, CreateProjectResponse>
{
    public async Task<CreateProjectResponse> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(RoleNames.SystemAdministrator) &&
            !currentUser.IsInRole(RoleNames.DepartmentManager))
            throw new ForbiddenAccessException("Only System Administrators or Department Managers can create projects.");

        var normalizedKey = request.ProjectKey.ToUpperInvariant();

        var keyExists = await db.Projects.AnyAsync(p => p.ProjectKey == normalizedKey && !p.IsDeleted, cancellationToken);
        if (keyExists)
            throw new ConflictException($"A project with key '{normalizedKey}' already exists.");

        _ = await db.Users.FindAsync([request.LeadUserId], cancellationToken)
            ?? throw new NotFoundException("User", request.LeadUserId);

        _ = await db.Departments.FindAsync([request.DepartmentId], cancellationToken)
            ?? throw new NotFoundException("Department", request.DepartmentId);

        if (request.WorkflowId.HasValue)
            _ = await db.Workflows.FindAsync([request.WorkflowId.Value], cancellationToken)
                ?? throw new NotFoundException("Workflow", request.WorkflowId.Value);

        var project = Project.Create(request.ProjectKey, request.Name, request.Description, request.LeadUserId, request.DepartmentId);

        if (request.WorkflowId.HasValue)
            project.AssignWorkflow(request.WorkflowId.Value);

        db.Projects.Add(project);

        // Initialize ticket sequence so NextNumberAsync works immediately
        db.ProjectSequences.Add(ProjectSequence.CreateForProject(project.Id));

        // Auto-add lead as Project Lead member
        var leadRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.ProjectLead, cancellationToken);
        if (leadRole is not null)
            db.ProjectMembers.Add(ProjectMember.Add(project.Id, request.LeadUserId, leadRole.Id, currentUser.UserId));

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            AuditOperation.Create, "Project", project.Id.ToString(),
            project.Id, null,
            JsonSerializer.Serialize(new { project.ProjectKey, project.Name, project.DepartmentId }),
            cancellationToken: cancellationToken);

        return new CreateProjectResponse(project.Id, project.ProjectKey, project.Name);
    }
}
