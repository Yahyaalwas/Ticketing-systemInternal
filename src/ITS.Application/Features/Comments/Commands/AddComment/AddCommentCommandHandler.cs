using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Content;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Comments.Commands.AddComment;

public sealed class AddCommentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IMarkdownService markdownService,
    INotificationService notifications)
    : IRequestHandler<AddCommentCommand, AddCommentResponse>
{
    public async Task<AddCommentResponse> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .Include(t => t.Watchers)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to comment on this ticket.");

        // Validate parent comment exists if threaded reply
        if (request.ParentCommentId.HasValue)
        {
            var parent = await db.Comments.FirstOrDefaultAsync(
                c => c.Id == request.ParentCommentId.Value && !c.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Comment", request.ParentCommentId.Value);

            if (parent.TicketId != request.TicketId)
                throw new ForbiddenAccessException("Parent comment does not belong to this ticket.");
        }

        // Render markdown and extract @mentions
        var html = markdownService.RenderToHtml(request.Body);
        markdownService.ExtractMentions(request.Body, out var mentionedUpns);

        // Create comment
        var comment = Comment.Create(ticket.Id, request.ParentCommentId, currentUser.UserId, request.Body, html);

        // Resolve mentions to users
        if (mentionedUpns.Count > 0)
        {
            var mentionedUsers = await db.Users.AsNoTracking()
                .Where(u => mentionedUpns.Contains(u.UserPrincipalName) && u.IsActive && !u.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var user in mentionedUsers)
            {
                comment.AddMention(user.Id);
            }

            // Notify mentioned users
            await notifications.NotifyManyAsync(
                mentionedUsers.Select(u => u.Id),
                NotificationType.Mentioned,
                $"You were mentioned in a comment",
                request.Body[..Math.Min(200, request.Body.Length)],
                ticket.Id, ticket.ProjectId, currentUser.UserId,
                cancellationToken);
        }

        db.Comments.Add(comment);

        // Notify ticket watchers (excluding commenter)
        var watcherIds = ticket.Watchers
            .Where(w => w.UserId != currentUser.UserId)
            .Select(w => w.UserId)
            .ToList();

        if (watcherIds.Count > 0)
        {
            await notifications.NotifyManyAsync(
                watcherIds,
                NotificationType.CommentAdded,
                $"New comment on ticket",
                request.Body[..Math.Min(200, request.Body.Length)],
                ticket.Id, ticket.ProjectId, currentUser.UserId,
                cancellationToken);
        }

        // Activity log
        db.ActivityLogs.Add(Domain.Entities.Audit.ActivityLog.Create(
            ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.CommentAdded, "Comment", comment.Id.ToString(),
            null, null, null));

        await db.SaveChangesAsync(cancellationToken);

        return new AddCommentResponse(comment.Id, html);
    }
}
