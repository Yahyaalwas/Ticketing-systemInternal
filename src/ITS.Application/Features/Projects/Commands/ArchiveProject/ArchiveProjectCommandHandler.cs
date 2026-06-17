using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Projects.Commands.ArchiveProject;

public sealed class ArchiveProjectCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime,
    IAuditService auditService)
    : IRequestHandler<ArchiveProjectCommand>
{
    public async Task Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (!currentUser.IsInRole(RoleNames.SystemAdministrator) &&
            project.LeadUserId != currentUser.UserId)
            throw new ForbiddenAccessException("Only the project lead or a system administrator can archive this project.");

        project.Archive(currentUser.UserId, dateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(AuditOperation.Archive, "Project", project.Id.ToString(), project.Id, cancellationToken: cancellationToken);
    }
}
