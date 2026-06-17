using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Queries.GetKanbanBoard;

public sealed class GetKanbanBoardQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IDateTimeService dateTime)
    : IRequestHandler<GetKanbanBoardQuery, KanbanBoardDto>
{
    public async Task<KanbanBoardDto> Handle(GetKanbanBoardQuery request, CancellationToken cancellationToken)
    {
        if (!await authz.CanViewProjectAsync(currentUser.UserId, request.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to view this project's board.");

        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (project.ActiveWorkflowId is null)
            throw new Domain.Exceptions.DomainException("Project does not have an active workflow configured.");

        // Load workflow statuses ordered for board columns
        var statuses = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => s.WorkflowId == project.ActiveWorkflowId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Load all non-deleted tickets for this project with filters applied
        var ticketsQuery = db.Tickets.AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId && !t.IsDeleted);

        if (request.AssigneeUserId.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.AssigneeUserId == request.AssigneeUserId.Value);

        if (request.PriorityId.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.PriorityId == request.PriorityId.Value);

        if (request.EpicTicketId.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.EpicTicketId == request.EpicTicketId.Value);

        if (request.LabelId.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.Labels.Any(l => l.LabelId == request.LabelId.Value));

        if (request.IssueTypeId.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.IssueTypeId == request.IssueTypeId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            if (int.TryParse(request.Search, out var ticketNum))
                ticketsQuery = ticketsQuery.Where(t => t.TicketNumber == ticketNum || t.Title.Contains(request.Search));
            else
                ticketsQuery = ticketsQuery.Where(t => t.Title.Contains(request.Search));
        }

        var tickets = await ticketsQuery
            .Include(t => t.Labels)
            .ToListAsync(cancellationToken);

        // Load reference data in bulk for efficiency
        var issueTypeIds = tickets.Select(t => t.IssueTypeId).Distinct().ToList();
        var priorityIds = tickets.Where(t => t.PriorityId.HasValue).Select(t => t.PriorityId!.Value).Distinct().ToList();
        var assigneeIds = tickets.Where(t => t.AssigneeUserId.HasValue).Select(t => t.AssigneeUserId!.Value).Distinct().ToList();
        var labelIdsNeeded = tickets.SelectMany(t => t.Labels.Select(l => l.LabelId)).Distinct().ToList();

        var issueTypes = await db.IssueTypes.AsNoTracking()
            .Where(it => issueTypeIds.Contains(it.Id))
            .ToListAsync(cancellationToken);

        var priorities = await db.Priorities.AsNoTracking()
            .Where(p => priorityIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var assignees = await db.Users.AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var labelsForTickets = await db.Labels.AsNoTracking()
            .Where(l => labelIdsNeeded.Contains(l.Id))
            .ToListAsync(cancellationToken);

        // Comment and attachment counts in bulk
        var ticketIds = tickets.Select(t => t.Id).ToList();

        var commentCounts = await db.Comments.AsNoTracking()
            .Where(c => ticketIds.Contains(c.TicketId) && !c.IsDeleted)
            .GroupBy(c => c.TicketId)
            .Select(g => new { TicketId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TicketId, x => x.Count, cancellationToken);

        var attachmentCounts = await db.Attachments.AsNoTracking()
            .Where(a => ticketIds.Contains(a.TicketId) && !a.IsDeleted)
            .GroupBy(a => a.TicketId)
            .Select(g => new { TicketId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TicketId, x => x.Count, cancellationToken);

        var now = dateTime.UtcNow;

        // Build columns
        var columns = statuses.Select(status =>
        {
            var columnTickets = tickets
                .Where(t => t.StatusId == status.Id)
                .OrderBy(t => t.UpdatedAt)
                .Select(t =>
                {
                    var it = issueTypes.FirstOrDefault(i => i.Id == t.IssueTypeId);
                    var pr = t.PriorityId.HasValue ? priorities.FirstOrDefault(p => p.Id == t.PriorityId.Value) : null;
                    var assignee = t.AssigneeUserId.HasValue ? assignees.FirstOrDefault(u => u.Id == t.AssigneeUserId.Value) : null;
                    var ticketLabels = t.Labels.Select(tl => labelsForTickets.FirstOrDefault(l => l.Id == tl.LabelId)?.Name ?? "")
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToList();

                    return new KanbanTicketDto(
                        t.Id,
                        t.TicketNumber,
                        $"{project.ProjectKey}-{t.TicketNumber}",
                        t.Title,
                        it?.Name ?? "Unknown",
                        it?.IconUrl,
                        pr?.Name,
                        pr?.Color,
                        t.AssigneeUserId,
                        assignee?.DisplayName,
                        assignee?.AvatarUrl,
                        t.DueDate,
                        t.StoryPoints,
                        t.SlaBreachAt,
                        t.SlaBreachAt.HasValue && t.SlaBreachAt.Value < now,
                        ticketLabels,
                        commentCounts.GetValueOrDefault(t.Id),
                        attachmentCounts.GetValueOrDefault(t.Id),
                        t.RowVersion);
                })
                .ToList();

            return new KanbanColumnDto(
                status.Id,
                status.Name,
                status.Category.ToString(),
                status.Color,
                status.DisplayOrder,
                null, // WIP limit would come from project board config
                columnTickets);
        }).ToList();

        return new KanbanBoardDto(project.Id, project.ProjectKey, project.Name, columns);
    }
}
