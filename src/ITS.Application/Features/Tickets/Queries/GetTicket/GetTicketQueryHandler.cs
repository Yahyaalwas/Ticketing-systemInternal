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

        // Build the DTO using projections against related tables
        var project = await db.Projects.AsNoTracking()
            .FirstAsync(p => p.Id == ticket.ProjectId, cancellationToken);

        var status = await db.WorkflowStatuses.AsNoTracking()
            .FirstAsync(s => s.Id == ticket.StatusId, cancellationToken);

        var issueType = await db.IssueTypes.AsNoTracking()
            .FirstAsync(it => it.Id == ticket.IssueTypeId, cancellationToken);

        var assignee = ticket.AssigneeUserId.HasValue
            ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ticket.AssigneeUserId.Value, cancellationToken)
            : null;

        var reporter = await db.Users.AsNoTracking()
            .FirstAsync(u => u.Id == ticket.ReporterUserId, cancellationToken);

        var createdBy = await db.Users.AsNoTracking()
            .FirstAsync(u => u.Id == ticket.CreatedByUserId, cancellationToken);

        var priority = ticket.PriorityId.HasValue
            ? await db.Priorities.AsNoTracking().FirstOrDefaultAsync(p => p.Id == ticket.PriorityId.Value, cancellationToken)
            : null;

        var resolution = ticket.ResolutionId.HasValue
            ? await db.Resolutions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == ticket.ResolutionId.Value, cancellationToken)
            : null;

        // Labels
        var labelIds = ticket.Labels.Select(l => l.LabelId).ToList();
        var labels = await db.Labels.AsNoTracking()
            .Where(l => labelIds.Contains(l.Id))
            .Select(l => new LabelDto(l.Id, l.Name, l.Color))
            .ToListAsync(cancellationToken);

        // Links (both directions)
        var outboundLinks = await db.TicketLinks.AsNoTracking()
            .Include(l => l.LinkType)
            .Where(l => l.SourceTicketId == ticket.Id)
            .ToListAsync(cancellationToken);

        var inboundLinks = await db.TicketLinks.AsNoTracking()
            .Include(l => l.LinkType)
            .Where(l => l.TargetTicketId == ticket.Id)
            .ToListAsync(cancellationToken);

        var linkedTicketIds = outboundLinks.Select(l => l.TargetTicketId)
            .Concat(inboundLinks.Select(l => l.SourceTicketId))
            .Distinct()
            .ToList();

        var linkedTickets = await db.Tickets.AsNoTracking()
            .Include(t => t.Labels)
            .Where(t => linkedTicketIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var linkedProjects = await db.Projects.AsNoTracking()
            .Where(p => linkedTickets.Select(t => t.ProjectId).Distinct().Contains(p.Id))
            .ToListAsync(cancellationToken);

        var links = outboundLinks
            .Select(l =>
            {
                var lt = linkedTickets.First(t => t.Id == l.TargetTicketId);
                var lp = linkedProjects.First(p => p.Id == lt.ProjectId);
                return new TicketLinkDto(l.Id, lt.Id, $"{lp.ProjectKey}-{lt.TicketNumber}", lt.Title, l.LinkType.OutwardName);
            })
            .Concat(inboundLinks.Select(l =>
            {
                var lt = linkedTickets.First(t => t.Id == l.SourceTicketId);
                var lp = linkedProjects.First(p => p.Id == lt.ProjectId);
                return new TicketLinkDto(l.Id, lt.Id, $"{lp.ProjectKey}-{lt.TicketNumber}", lt.Title, l.LinkType.InwardName);
            }))
            .ToList();

        // Watchers
        var watcherIds = ticket.Watchers.Select(w => w.UserId).ToList();
        var watchers = await db.Users.AsNoTracking()
            .Where(u => watcherIds.Contains(u.Id))
            .Select(u => new TicketWatcherDto(u.Id, u.DisplayName))
            .ToListAsync(cancellationToken);

        var commentCount = await db.Comments.CountAsync(c => c.TicketId == ticket.Id && !c.IsDeleted, cancellationToken);
        var attachmentCount = await db.Attachments.CountAsync(a => a.TicketId == ticket.Id && !a.IsDeleted, cancellationToken);

        // Parent and Epic key lookups
        string? parentKey = null;
        if (ticket.ParentTicketId.HasValue)
        {
            var parent = await db.Tickets.AsNoTracking()
                .Include(t => t.Labels)
                .FirstOrDefaultAsync(t => t.Id == ticket.ParentTicketId.Value, cancellationToken);
            if (parent != null)
            {
                var parentProject = linkedProjects.FirstOrDefault(p => p.Id == parent.ProjectId)
                    ?? await db.Projects.AsNoTracking().FirstAsync(p => p.Id == parent.ProjectId, cancellationToken);
                parentKey = $"{parentProject.ProjectKey}-{parent.TicketNumber}";
            }
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
            null, // EpicTicketKey — resolved similarly to parentKey
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
            commentCount,
            attachmentCount);
    }
}
