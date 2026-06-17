using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Projects;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Projects.Commands.AddMember;

public sealed class AddMemberCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IRequestHandler<AddMemberCommand, AddMemberResponse>
{
    public async Task<AddMemberResponse> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (!currentUser.IsInRole(RoleNames.SystemAdministrator) &&
            project.LeadUserId != currentUser.UserId)
            throw new ForbiddenAccessException("Only the project lead or a system administrator can add members.");

        _ = await db.Users.FindAsync([request.UserId], cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        _ = await db.Roles.FindAsync([request.RoleId], cancellationToken)
            ?? throw new NotFoundException("Role", request.RoleId);

        var existing = await db.ProjectMembers
            .AnyAsync(m => m.ProjectId == request.ProjectId && m.UserId == request.UserId, cancellationToken);
        if (existing)
            throw new ConflictException($"User is already a member of this project.");

        var member = ProjectMember.Add(request.ProjectId, request.UserId, request.RoleId, currentUser.UserId);
        db.ProjectMembers.Add(member);

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(AuditOperation.PermissionGrant, "ProjectMember", member.Id.ToString(), request.ProjectId, cancellationToken: cancellationToken);

        return new AddMemberResponse(member.Id);
    }
}
