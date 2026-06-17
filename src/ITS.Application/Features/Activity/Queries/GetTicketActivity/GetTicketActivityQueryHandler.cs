using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Activity.Queries.GetTicketActivity;

public sealed class GetTicketActivityQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<GetTicketActivityQuery, TicketActivityResult>
{
    public async Task<TicketActivityResult> Handle(GetTicketActivityQuery request, CancellationToken cancellationToken)
    {
        // Resolve the ticket's project so we can do an authorization check.
        var ticket = await db.Tickets.AsNoTracking()
            .Where(t => t.Id == request.TicketId && !t.IsDeleted)
            .Select(t => new { t.Id, t.ProjectId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to view this ticket's activity.");

        // Count total activity entries for this ticket.
        var totalCount = await db.ActivityLogs.AsNoTracking()
            .CountAsync(a => a.TicketId == request.TicketId, cancellationToken);

        if (totalCount == 0)
            return new TicketActivityResult([], 0, request.PageNumber, 0);

        // Load the requested page, ordered newest-first.
        var logs = await db.ActivityLogs.AsNoTracking()
            .Where(a => a.TicketId == request.TicketId)
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                a.Id,
                a.ActivityType,
                a.EntityType,
                a.FieldName,
                a.OldValue,
                a.NewValue,
                a.ActorUserId,
                a.OccurredAt
            })
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            var emptyPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
            return new TicketActivityResult([], totalCount, request.PageNumber, emptyPages);
        }

        // Batch-load actor display names and avatars in a single query.
        var actorIds = logs.Select(l => l.ActorUserId).Distinct().ToList();

        var actors = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.AvatarUrl })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = logs.Select(l =>
        {
            actors.TryGetValue(l.ActorUserId, out var actor);
            return new ActivityItemDto(
                l.Id,
                l.ActivityType.ToString(),
                l.EntityType,
                l.FieldName,
                l.OldValue,
                l.NewValue,
                l.ActorUserId,
                actor?.DisplayName ?? "Unknown",
                actor?.AvatarUrl,
                l.OccurredAt);
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new TicketActivityResult(
            items.AsReadOnly(),
            totalCount,
            request.PageNumber,
            totalPages);
    }
}
