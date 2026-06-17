using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IDateTimeService dateTime)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int MaxItems = 20;

    // Concrete projection used for all ticket list queries so we avoid dynamic.
    private sealed record TicketRow(
        Guid Id,
        int TicketNumber,
        string Title,
        Guid ProjectId,
        int StatusId,
        int? PriorityId,
        Guid? AssigneeUserId,
        DateOnly? DueDate,
        DateTime? SlaBreachAt,
        DateTime UpdatedAt);

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = dateTime.UtcNow;
        var today = dateTime.TodayUtc;
        var userId = currentUser.UserId;

        var accessibleIds = await authz.GetAccessibleProjectIdsAsync(userId, cancellationToken);

        var baseQuery = db.Tickets.AsNoTracking()
            .Where(t => !t.IsDeleted && accessibleIds.Contains(t.ProjectId));

        // ── Ticket list queries ─────────────────────────────────────────────

        var assignedToMeTask = baseQuery
            .Where(t => t.AssigneeUserId == userId)
            .OrderBy(t => t.DueDate)
            .Take(MaxItems)
            .Select(t => new TicketRow(
                t.Id, t.TicketNumber, t.Title, t.ProjectId,
                t.StatusId, t.PriorityId, t.AssigneeUserId,
                t.DueDate, t.SlaBreachAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        var reportedByMeTask = baseQuery
            .Where(t => t.ReporterUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(MaxItems)
            .Select(t => new TicketRow(
                t.Id, t.TicketNumber, t.Title, t.ProjectId,
                t.StatusId, t.PriorityId, t.AssigneeUserId,
                t.DueDate, t.SlaBreachAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        var overdueTask = baseQuery
            .Where(t => t.DueDate.HasValue && t.DueDate.Value < today)
            .OrderBy(t => t.DueDate)
            .Take(MaxItems)
            .Select(t => new TicketRow(
                t.Id, t.TicketNumber, t.Title, t.ProjectId,
                t.StatusId, t.PriorityId, t.AssigneeUserId,
                t.DueDate, t.SlaBreachAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        var recentTask = baseQuery
            .OrderByDescending(t => t.UpdatedAt)
            .Take(MaxItems)
            .Select(t => new TicketRow(
                t.Id, t.TicketNumber, t.Title, t.ProjectId,
                t.StatusId, t.PriorityId, t.AssigneeUserId,
                t.DueDate, t.SlaBreachAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        var myOpenTask = baseQuery
            .Where(t => t.AssigneeUserId == userId || t.ReporterUserId == userId)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(MaxItems)
            .Select(t => new TicketRow(
                t.Id, t.TicketNumber, t.Title, t.ProjectId,
                t.StatusId, t.PriorityId, t.AssigneeUserId,
                t.DueDate, t.SlaBreachAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        // ── Breakdown queries ───────────────────────────────────────────────

        var priorityBreakdownTask = baseQuery
            .GroupBy(t => t.PriorityId)
            .Select(g => new { PriorityId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var statusBreakdownTask = baseQuery
            .GroupBy(t => t.StatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var projectBreakdownTask = baseQuery
            .GroupBy(t => t.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(
            assignedToMeTask,
            reportedByMeTask,
            overdueTask,
            recentTask,
            myOpenTask,
            priorityBreakdownTask,
            statusBreakdownTask,
            projectBreakdownTask);

        var assignedToMe   = await assignedToMeTask;
        var reportedByMe   = await reportedByMeTask;
        var overdue        = await overdueTask;
        var recent         = await recentTask;
        var myOpen         = await myOpenTask;
        var priorityBreaks = await priorityBreakdownTask;
        var statusBreaks   = await statusBreakdownTask;
        var projectBreaks  = await projectBreakdownTask;

        // ── Batch-load reference data ───────────────────────────────────────

        var allRows = assignedToMe
            .Concat(reportedByMe)
            .Concat(overdue)
            .Concat(recent)
            .Concat(myOpen);

        var projectIds = allRows.Select(r => r.ProjectId)
            .Concat(projectBreaks.Select(p => p.ProjectId))
            .Distinct().ToList();

        var statusIds = allRows.Select(r => r.StatusId)
            .Concat(statusBreaks.Select(s => s.StatusId))
            .Distinct().ToList();

        var priorityIds = allRows
            .Where(r => r.PriorityId.HasValue).Select(r => r.PriorityId!.Value)
            .Concat(priorityBreaks.Where(p => p.PriorityId.HasValue).Select(p => p.PriorityId!.Value))
            .Distinct().ToList();

        var assigneeIds = allRows
            .Where(r => r.AssigneeUserId.HasValue).Select(r => r.AssigneeUserId!.Value)
            .Distinct().ToList();

        var projectsTask = db.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ProjectKey })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var statusesTask = db.WorkflowStatuses.AsNoTracking()
            .Where(s => statusIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, Category = s.Category.ToString() })
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var prioritiesTask = db.Priorities.AsNoTracking()
            .Where(p => priorityIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Color })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var assigneesTask = db.Users.AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        await Task.WhenAll(projectsTask, statusesTask, prioritiesTask, assigneesTask);

        var projects   = await projectsTask;
        var statuses   = await statusesTask;
        var priorities = await prioritiesTask;
        var assignees  = await assigneesTask;

        // ── Local mapper ────────────────────────────────────────────────────

        DashboardTicketDto MapRow(TicketRow t)
        {
            projects.TryGetValue(t.ProjectId, out var proj);
            statuses.TryGetValue(t.StatusId, out var status);
            var priority = t.PriorityId.HasValue && priorities.TryGetValue(t.PriorityId.Value, out var p) ? p : null;
            var assigneeName = t.AssigneeUserId.HasValue
                ? assignees.GetValueOrDefault(t.AssigneeUserId.Value)
                : null;

            return new DashboardTicketDto(
                t.Id,
                proj is not null ? $"{proj.ProjectKey}-{t.TicketNumber}" : t.TicketNumber.ToString(),
                t.Title,
                proj?.Name ?? "",
                status?.Name ?? "",
                priority?.Name,
                priority?.Color,
                t.AssigneeUserId,
                assigneeName,
                t.DueDate,
                t.SlaBreachAt,
                t.SlaBreachAt.HasValue && t.SlaBreachAt.Value < now,
                t.UpdatedAt);
        }

        // ── Breakdowns ──────────────────────────────────────────────────────

        var byPriority = priorityBreaks.Select(p =>
        {
            var name = p.PriorityId.HasValue && priorities.TryGetValue(p.PriorityId.Value, out var pr)
                ? pr.Name
                : "None";
            return new PriorityBreakdownDto(p.PriorityId, name, p.Count);
        }).ToList();

        var byStatus = statusBreaks.Select(s =>
        {
            statuses.TryGetValue(s.StatusId, out var st);
            return new StatusBreakdownDto(s.StatusId, st?.Name ?? "", st?.Category ?? "", s.Count);
        }).ToList();

        var byProject = projectBreaks.Select(p =>
        {
            projects.TryGetValue(p.ProjectId, out var proj);
            return new ProjectBreakdownDto(p.ProjectId, proj?.Name ?? "", p.Count);
        }).ToList();

        return new DashboardDto(
            myOpen.Select(MapRow).ToList().AsReadOnly(),
            assignedToMe.Select(MapRow).ToList().AsReadOnly(),
            reportedByMe.Select(MapRow).ToList().AsReadOnly(),
            overdue.Select(MapRow).ToList().AsReadOnly(),
            recent.Select(MapRow).ToList().AsReadOnly(),
            byPriority.AsReadOnly(),
            byStatus.AsReadOnly(),
            byProject.AsReadOnly());
    }
}
