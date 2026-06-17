using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Attachments.Commands.DeleteAttachment;

public sealed class DeleteAttachmentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IDateTimeService dateTime)
    : IRequestHandler<DeleteAttachmentCommand>
{
    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        var attachment = await db.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.TicketId == request.TicketId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Attachment", request.AttachmentId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken) &&
            attachment.UploaderUserId != currentUser.UserId)
            throw new ForbiddenAccessException("You do not have permission to delete this attachment.");

        attachment.SoftDelete(currentUser.UserId, dateTime.UtcNow);

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.AttachmentDeleted, "Attachment", attachment.Id.ToString(),
            "FileName", attachment.FileName, null));

        await db.SaveChangesAsync(cancellationToken);
    }
}
