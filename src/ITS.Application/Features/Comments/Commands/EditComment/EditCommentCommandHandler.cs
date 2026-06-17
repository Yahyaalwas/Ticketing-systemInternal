using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Comments.Commands.EditComment;

public sealed class EditCommentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IMarkdownService markdownService)
    : IRequestHandler<EditCommentCommand>
{
    public async Task Handle(EditCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await db.Comments
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        if (comment.AuthorUserId != currentUser.UserId && !currentUser.IsInRole(Shared.Constants.RoleNames.SystemAdministrator))
            throw new ForbiddenAccessException("You can only edit your own comments.");

        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == comment.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", comment.TicketId);

        var html = markdownService.RenderToHtml(request.Body);
        var history = comment.Edit(request.Body, html, currentUser.UserId);

        db.CommentHistories.Add(history);

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.CommentEdited, "Comment", comment.Id.ToString(), null, null, null));

        await db.SaveChangesAsync(cancellationToken);
    }
}
