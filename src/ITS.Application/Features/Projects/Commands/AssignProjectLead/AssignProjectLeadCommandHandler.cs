using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ITS.Application.Features.Projects.Commands.AssignProjectLead;

public sealed class AssignProjectLeadCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IRequestHandler<AssignProjectLeadCommand>
{
    public async Task Handle(AssignProjectLeadCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (!currentUser.IsInRole(RoleNames.SystemAdministrator) &&
            project.LeadUserId != currentUser.UserId)
            throw new ForbiddenAccessException("Only the current project lead or a system administrator can reassign the project lead.");

        _ = await db.Users.FindAsync([request.NewLeadUserId], cancellationToken)
            ?? throw new NotFoundException("User", request.NewLeadUserId);

        var before = project.LeadUserId.ToString();
        project.Update(project.Name, project.Description, request.NewLeadUserId, project.AvatarUrl);

        // Ensure new lead is a project member with ProjectLead role
        var existingMember = await db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == request.ProjectId && m.UserId == request.NewLeadUserId, cancellationToken);

        if (existingMember is null)
        {
            var leadRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.ProjectLead, cancellationToken);
            if (leadRole is not null)
                db.ProjectMembers.Add(Domain.Entities.Projects.ProjectMember.Add(project.Id, request.NewLeadUserId, leadRole.Id, currentUser.UserId));
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(AuditOperation.Update, "Project", project.Id.ToString(), project.Id,
            JsonSerializer.Serialize(new { LeadUserId = before }),
            JsonSerializer.Serialize(new { LeadUserId = request.NewLeadUserId }),
            cancellationToken: cancellationToken);
    }
}
