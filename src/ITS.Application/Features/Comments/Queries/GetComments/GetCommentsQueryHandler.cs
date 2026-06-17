using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<GetCommentsQuery, CommentListResult>
{
    public async Task<CommentListResult> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to view comments on this ticket.");

        var totalCount = await db.Comments.CountAsync(c => c.TicketId == request.TicketId && !c.IsDeleted, cancellationToken);

        var comments = await db.Comments.AsNoTracking()
            .Include(c => c.Mentions)
            .Where(c => c.TicketId == request.TicketId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        if (comments.Count == 0)
            return new CommentListResult([], totalCount, request.PageNumber, (int)Math.Ceiling(totalCount / (double)request.PageSize));

        var authorIds = comments.Select(c => c.AuthorUserId).Distinct().ToList();
        var authors = await db.Users.AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.AvatarUrl })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = comments.Select(c =>
        {
            authors.TryGetValue(c.AuthorUserId, out var author);
            return new CommentDto(
                c.Id, c.ParentCommentId, c.AuthorUserId,
                author?.DisplayName ?? "Unknown", author?.AvatarUrl,
                c.Body, c.BodyHtml, c.IsEdited,
                c.CreatedAt, c.UpdatedAt,
                c.Mentions.Select(m => m.MentionedUserId).ToList().AsReadOnly());
        }).ToList().AsReadOnly();

        return new CommentListResult(items, totalCount, request.PageNumber, (int)Math.Ceiling(totalCount / (double)request.PageSize));
    }
}
