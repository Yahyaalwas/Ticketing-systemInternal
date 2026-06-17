using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Queries.GetTicket;

public sealed class GetTicketQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<GetTicketQuery, TicketDetailDto>
{
    public async Task<TicketDetailDto> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .Include(t => t.Labels)
            .Include(t => t.Watchers)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Tickets.Ticket), request.TicketId);

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to view this ticket.");

        // Batch 1: fire all independent scalar lookups concurrently
        var projectTask    = db.Projects.AsNoTracking().FirstAsync(p => p.Id == ticket.ProjectId, cancellationToken);
        var statusTask     = db.WorkflowStatuses.AsNoTracking().FirstAsync(s => s.Id == ticket.StatusId, cancellationToken);
        var issueTypeTask  = db.IssueTypes.AsNoTracking().FirstAsync(it => it.Id == ticket.IssueTypeId, cancellationToken);

        var priorityTask   = ticket.PriorityId.HasValue
            ? db.Priorities.AsNoTracking().FirstOrDefaultAsync(p => p.Id == ticket.PriorityId.Value, cancellationToken)
            : Task.FromResult<Domain.Entities.Projects.Priority?>(null);

        var assigneeTask   = ticket.AssigneeUserId.HasValue
            ? db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ticket.AssigneeUserId.Value, cancellationToken)
            : Task.FromResult<Domain.Entities.Identity.User?>(null);

        var reporterTask   = db.Users.AsNoTracking().FirstAsync(u => u.Id == ticket.ReporterUserId, cancellationToken);
        var createdByTask  = db.Users.AsNoTracking().FirstAsync(u => u.Id == ticket.CreatedByUserId, cancellationToken);

        var resolutionTask = ticket.ResolutionId.HasValue
            ? db.Resolutions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == ticket.ResolutionId.Value, cancellationToken)
            : Task.FromResult<Domain.Entities.Tickets.Resolution?>(null);

        var commentCountTask    = db.Comments.CountAsync(c => c.TicketId == ticket.Id && !c.IsDeleted, cancellationToken);
        var attachmentCountTask = db.Attachments.CountAsync(a => a.TicketId == ticket.Id && !a.IsDeleted, cancellationToken);

        await Task.WhenAll(projectTask, statusTask, issueTypeTask, priorityTask,
                           assigneeTask, reporterTask, createdByTask, resolutionTask,
                           commentCountTask, attachmentCountTask);

        var project    = await projectTask;
        var status     = await statusTask;
        var issueType  = await issueTypeTask;
        var priority   = await priorityTask;
        var assignee   = await assigneeTask;
        var reporter   = await reporterTask;
        var createdBy  = await createdByTask;
        var resolution = await resolutionTask;

        // Batch 2: labels
        var labelIds = ticket.Labels.Select(l => l.LabelId).ToList();
        var labels = labelIds.Count == 0
            ? (IReadOnlyList<LabelDto>)[]
            : await db.Labels.AsNoTracking()
                .Where(l => labelIds.Contains(l.Id))
                .Select(l => new LabelDto(l.Id, l.Name, l.Color))
                .ToListAsync(cancellationToken);

        // Batch 3: links — load both directions then resolve referenced tickets/projects in two queries
        var outboundLinks = await db.TicketLinks.AsNoTracking()
            .Include(l => l.LinkType)
            .Where(l => l.SourceTicketId == ticket.Id)
            .ToListAsync(cancellationToken);

        var inboundLinks = await db.TicketLinks.AsNoTracking()
            .Include(l => l.LinkType)
            .Where(l => l.TargetTicketId == ticket.Id)
            .ToListAsync(cancellationToken);

        IReadOnlyList<TicketLinkDto> links;
        var linkedTicketIds = outboundLinks.Select(l => l.TargetTicketId)
            .Concat(inboundLinks.Select(l => l.SourceTicketId))
            .Distinct()
            .ToList();

        if (linkedTicketIds.Count == 0)
        {
            links = [];
        }
        else
        {
            var linkedTickets = await db.Tickets.AsNoTracking()
                .Where(t => linkedTicketIds.Contains(t.Id))
                .Select(t => new { t.Id, t.ProjectId, t.TicketNumber, t.Title })
                .ToListAsync(cancellationToken);

            var linkedProjectIds = linkedTickets.Select(t => t.ProjectId).Distinct().ToList();
            var linkedProjects = await db.Projects.AsNoTracking()
                .Where(p => linkedProjectIds.Contains(p.Id))
                .Select(p => new { p.Id, p.ProjectKey })
                .ToListAsync(cancellationToken);

            var ticketMap  = linkedTickets.ToDictionary(t => t.Id);
            var projectMap = linkedProjects.ToDictionary(p => p.Id);

            links = outboundLinks
                .Select(l =>
                {
                    var lt = ticketMap[l.TargetTicketId];
                    var lp = projectMap[lt.ProjectId];
                    return new TicketLinkDto(l.Id, lt.Id, $"{lp.ProjectKey}-{lt.TicketNumber}", lt.Title, l.LinkType.OutwardName);
                })
                .Concat(inboundLinks.Select(l =>
                {
                    var lt = ticketMap[l.SourceTicketId];
                    var lp = projectMap[lt.ProjectId];
                    return new TicketLinkDto(l.Id, lt.Id, $"{lp.ProjectKey}-{lt.TicketNumber}", lt.Title, l.LinkType.InwardName);
                }))
                .ToList();
        }

        // Batch 4: watchers
        var watcherIds = ticket.Watchers.Select(w => w.UserId).ToList();
        var watchers = watcherIds.Count == 0
            ? (IReadOnlyList<TicketWatcherDto>)[]
            : await db.Users.AsNoTracking()
                .Where(u => watcherIds.Contains(u.Id))
                .Select(u => new TicketWatcherDto(u.Id, u.DisplayName))
                .ToListAsync(cancellationToken);

        // Parent key: single join query if needed
        string? parentKey = null;
        if (ticket.ParentTicketId.HasValue)
        {
            parentKey = await (
                from t in db.Tickets.AsNoTracking()
                join p in db.Projects.AsNoTracking() on t.ProjectId equals p.Id
                where t.Id == ticket.ParentTicketId.Value
                select p.ProjectKey + "-" + t.TicketNumber.ToString()
            ).FirstOrDefaultAsync(cancellationToken);
        }

        return new TicketDetailDto(
            ticket.Id,
            ticket.ProjectId,
            project.ProjectKey,
            ticket.TicketNumber,
            $"{project.ProjectKey}-{ticket.TicketNumber}",
            ticket.Title,
            ticket.Description,
            ticket.DescriptionHtml,
            issueType.Name,
            status.Name,
            status.Category.ToString(),
            status.Color,
            priority?.Name,
            priority?.Color,
            ticket.AssigneeUserId,
            assignee?.DisplayName,
            assignee?.AvatarUrl,
            ticket.ReporterUserId,
            reporter.DisplayName,
            ticket.ParentTicketId,
            parentKey,
            ticket.EpicTicketId,
            null,
            ticket.DueDate,
            ticket.StoryPoints,
            ticket.EstimatedHours,
            ticket.ActualHours,
            resolution?.Name,
            ticket.ResolvedAt,
            ticket.SlaBreachAt,
            ticket.IsDeleted,
            ticket.CreatedAt,
            createdBy.DisplayName,
            ticket.UpdatedAt,
            ticket.RowVersion,
            labels,
            links,
            watchers,
            await commentCountTask,
            await attachmentCountTask);
    }
}
