using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime,
    IAuditService auditService)
    : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (!currentUser.IsInRole(RoleNames.SystemAdministrator))
            throw new ForbiddenAccessException("Only System Administrators can delete projects.");

        project.SoftDelete(currentUser.UserId, dateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(AuditOperation.Delete, "Project", project.Id.ToString(), project.Id, cancellationToken: cancellationToken);
    }
}
