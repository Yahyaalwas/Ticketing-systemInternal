using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Queries.ListTickets;

public sealed class ListTicketsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IDateTimeService dateTime)
    : IRequestHandler<ListTicketsQuery, TicketListResult>
{
    public async Task<TicketListResult> Handle(ListTicketsQuery request, CancellationToken cancellationToken)
    {
        if (!await authz.CanViewProjectAsync(currentUser.UserId, request.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to view this project.");

        var project = await db.Projects.AsNoTracking()
            .Where(p => p.Id == request.ProjectId && !p.IsDeleted)
            .Select(p => new { p.Id, p.ProjectKey })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var now = dateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var query = db.Tickets.AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId && !t.IsDeleted);

        if (request.StatusId.HasValue)
            query = query.Where(t => t.StatusId == request.StatusId.Value);

        if (request.PriorityId.HasValue)
            query = query.Where(t => t.PriorityId == request.PriorityId.Value);

        if (request.IssueTypeId.HasValue)
            query = query.Where(t => t.IssueTypeId == request.IssueTypeId.Value);

        if (request.AssigneeUserId.HasValue)
            query = query.Where(t => t.AssigneeUserId == request.AssigneeUserId.Value);

        if (request.LabelId.HasValue)
            query = query.Where(t => t.Labels.Any(l => l.LabelId == request.LabelId.Value));

        if (request.IsOverdue == true)
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < today);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            if (int.TryParse(request.Search, out var ticketNum))
                query = query.Where(t => t.TicketNumber == ticketNum || t.Title.Contains(request.Search));
            else
                query = query.Where(t => t.Title.Contains(request.Search));
        }

        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "created"  => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt)  : query.OrderBy(t => t.CreatedAt),
            "updated"  => request.SortDescending ? query.OrderByDescending(t => t.UpdatedAt)  : query.OrderBy(t => t.UpdatedAt),
            "priority" => request.SortDescending ? query.OrderByDescending(t => t.PriorityId) : query.OrderBy(t => t.PriorityId),
            "due"      => request.SortDescending ? query.OrderByDescending(t => t.DueDate)    : query.OrderBy(t => t.DueDate),
            _          => request.SortDescending ? query.OrderByDescending(t => t.UpdatedAt)  : query.OrderByDescending(t => t.UpdatedAt)
        };

        var totalCount = await sorted.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var tickets = await sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(t => t.Labels)
            .ToListAsync(cancellationToken);

        if (tickets.Count == 0)
            return new TicketListResult([], totalCount, request.PageNumber, totalPages);

        // Bulk-load reference data
        var issueTypeIds  = tickets.Select(t => t.IssueTypeId).Distinct().ToList();
        var statusIds     = tickets.Select(t => t.StatusId).Distinct().ToList();
        var priorityIds   = tickets.Where(t => t.PriorityId.HasValue).Select(t => t.PriorityId!.Value).Distinct().ToList();
        var assigneeIds   = tickets.Where(t => t.AssigneeUserId.HasValue).Select(t => t.AssigneeUserId!.Value).Distinct().ToList();

        var issueTypesTask = db.IssueTypes.AsNoTracking()
            .Where(it => issueTypeIds.Contains(it.Id))
            .Select(it => new { it.Id, it.Name, it.IconUrl })
            .ToDictionaryAsync(it => it.Id, cancellationToken);

        var statusesTask = db.WorkflowStatuses.AsNoTracking()
            .Where(s => statusIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, Category = s.Category.ToString(), s.Color })
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var prioritiesTask = db.Priorities.AsNoTracking()
            .Where(p => priorityIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Color })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var assigneesTask = db.Users.AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.AvatarUrl })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        await Task.WhenAll(issueTypesTask, statusesTask, prioritiesTask, assigneesTask);

        var issueTypes  = await issueTypesTask;
        var statuses    = await statusesTask;
        var priorities  = await prioritiesTask;
        var assignees   = await assigneesTask;

        var items = tickets.Select(t =>
        {
            issueTypes.TryGetValue(t.IssueTypeId, out var it);
            statuses.TryGetValue(t.StatusId, out var st);
            var pr = t.PriorityId.HasValue ? priorities.GetValueOrDefault(t.PriorityId.Value) : null;
            var assignee = t.AssigneeUserId.HasValue ? assignees.GetValueOrDefault(t.AssigneeUserId.Value) : null;

            return new TicketSummaryDto(
                t.Id,
                $"{project.ProjectKey}-{t.TicketNumber}",
                t.TicketNumber,
                t.Title,
                it?.Name ?? "Unknown",
                it?.IconUrl,
                st?.Name ?? "Unknown",
                st?.Category ?? "",
                st?.Color ?? null,
                pr?.Name,
                pr?.Color,
                t.AssigneeUserId,
                assignee?.DisplayName,
                assignee?.AvatarUrl,
                t.DueDate,
                t.SlaBreachAt,
                t.SlaBreachAt.HasValue && t.SlaBreachAt.Value < now,
                t.CreatedAt,
                t.UpdatedAt,
                t.RowVersion);
        }).ToList().AsReadOnly();

        return new TicketListResult(items, totalCount, request.PageNumber, totalPages);
    }
}
