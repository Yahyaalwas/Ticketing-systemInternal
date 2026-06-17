using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Projects.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (!currentUser.IsInRole(RoleNames.SystemAdministrator) &&
            project.LeadUserId != currentUser.UserId)
            throw new ForbiddenAccessException("Only the project lead or a system administrator can remove members.");

        var member = await db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == request.ProjectId && m.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException("ProjectMember", request.UserId);

        db.ProjectMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(AuditOperation.PermissionRevoke, "ProjectMember", member.Id.ToString(), request.ProjectId, cancellationToken: cancellationToken);
    }
}
