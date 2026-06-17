using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Projects.Commands.ChangeMemberRole;

public sealed class ChangeMemberRoleCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IRequestHandler<ChangeMemberRoleCommand>
{
    public async Task Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (!currentUser.IsInRole(RoleNames.SystemAdministrator) &&
            project.LeadUserId != currentUser.UserId)
            throw new ForbiddenAccessException("Only the project lead or a system administrator can change member roles.");

        _ = await db.Roles.FindAsync([request.NewRoleId], cancellationToken)
            ?? throw new NotFoundException("Role", request.NewRoleId);

        var member = await db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == request.ProjectId && m.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException("ProjectMember", request.UserId);

        member.ChangeRole(request.NewRoleId);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(AuditOperation.PermissionGrant, "ProjectMember", member.Id.ToString(), request.ProjectId, cancellationToken: cancellationToken);
    }
}
