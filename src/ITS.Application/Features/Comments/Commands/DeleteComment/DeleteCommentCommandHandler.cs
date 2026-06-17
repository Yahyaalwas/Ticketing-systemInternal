using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Comments.Commands.DeleteComment;

public sealed class DeleteCommentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime)
    : IRequestHandler<DeleteCommentCommand>
{
    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        if (comment.AuthorUserId != currentUser.UserId && !currentUser.IsInRole(Shared.Constants.RoleNames.SystemAdministrator))
            throw new ForbiddenAccessException("You can only delete your own comments.");

        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == comment.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", comment.TicketId);

        comment.SoftDelete(currentUser.UserId, dateTime.UtcNow);

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.CommentDeleted, "Comment", comment.Id.ToString(), null, null, null));

        await db.SaveChangesAsync(cancellationToken);
    }
}
