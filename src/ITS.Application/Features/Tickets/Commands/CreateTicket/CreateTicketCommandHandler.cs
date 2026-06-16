using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Tickets;
using MediatR;

namespace ITS.Application.Features.Tickets.Commands.CreateTicket;

public sealed class CreateTicketCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime,
    ITicketSequenceService sequenceService,
    IMarkdownService markdownService,
    IProjectAuthorizationService authz)
    : IRequestHandler<CreateTicketCommand, CreateTicketResponse>
{
    public async Task<CreateTicketResponse> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        // Authorization
        if (!await authz.CanCreateTicketAsync(currentUser.UserId, request.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to create tickets in this project.");

        // Validate project exists and is not archived
        var project = await db.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Projects.Project), request.ProjectId);

        if (project.IsDeleted || project.IsArchived)
            throw new ForbiddenAccessException("Cannot create tickets in an archived or deleted project.");

        // Validate issue type belongs to project
        var issueType = await db.IssueTypes.FindAsync([request.IssueTypeId], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Projects.IssueType), request.IssueTypeId);

        if (issueType.ProjectId != request.ProjectId)
            throw new ForbiddenAccessException("Issue type does not belong to the specified project.");

        // Validate workflow has an initial status
        if (project.ActiveWorkflowId is null)
            throw new Domain.Exceptions.DomainException("Project does not have an active workflow configured.");

        var workflow = await db.Workflows.FindAsync([project.ActiveWorkflowId], cancellationToken)
            ?? throw new NotFoundException("Workflow", project.ActiveWorkflowId);

        var initialStatus = workflow.GetInitialStatus()
            ?? throw new Domain.Exceptions.DomainException("Workflow has no initial status configured.");

        // Generate sequence number atomically
        var ticketNumber = await sequenceService.NextNumberAsync(request.ProjectId, cancellationToken);

        // Render description markdown
        string? descriptionHtml = null;
        if (!string.IsNullOrEmpty(request.Description))
            descriptionHtml = markdownService.RenderToHtml(request.Description);

        // Build SLA breach time from priority
        DateTime? slaBreachAt = null;
        if (request.PriorityId.HasValue)
        {
            var priority = await db.Priorities.FindAsync([request.PriorityId.Value], cancellationToken);
            if (priority?.SlaTargetHours.HasValue == true)
                slaBreachAt = dateTime.UtcNow.AddHours(priority.SlaTargetHours.Value);
        }

        // Create ticket
        var ticket = Ticket.Create(
            request.ProjectId,
            ticketNumber,
            request.Title,
            request.Description,
            request.IssueTypeId,
            initialStatus.Id,
            request.PriorityId,
            currentUser.UserId,
            request.AssigneeUserId,
            request.DueDate,
            request.StoryPoints,
            currentUser.UserId);

        ticket.UpdateDescription(request.Description, descriptionHtml);
        ticket.SetSlaBreachAt(slaBreachAt);

        if (request.ParentTicketId.HasValue)
            ticket.SetParent(request.ParentTicketId);

        if (request.EpicTicketId.HasValue)
            ticket.SetEpic(request.EpicTicketId);

        // Labels
        if (request.LabelIds?.Count > 0)
        {
            foreach (var labelId in request.LabelIds)
                ticket.Labels.Add(TicketLabel.Create(ticket.Id, labelId));
        }

        db.Tickets.Add(ticket);

        // Auto-watch by reporter
        ticket.AddWatcher(currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);

        return new CreateTicketResponse(
            ticket.Id,
            $"{project.ProjectKey}-{ticketNumber}",
            ticketNumber);
    }
}
