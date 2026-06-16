using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.TransitionTicket;

public sealed class TransitionTicketCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime,
    IWorkflowEngine workflowEngine,
    IProjectAuthorizationService authz,
    IMarkdownService markdownService)
    : IRequestHandler<TransitionTicketCommand>
{
    public async Task Handle(TransitionTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .Include(t => t.Watchers)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Tickets.Ticket), request.TicketId);

        // Concurrency check — EF Core will also catch this on SaveChanges via RowVersion
        if (!ticket.RowVersion.SequenceEqual(request.RowVersion))
            throw new ConflictException(nameof(Domain.Entities.Tickets.Ticket), request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to update tickets in this project.");

        // Load the project's workflow
        var project = await db.Projects.FindAsync([ticket.ProjectId], cancellationToken)
            ?? throw new NotFoundException("Project", ticket.ProjectId);

        if (project.ActiveWorkflowId is null)
            throw new DomainException("Project does not have an active workflow.");

        var workflow = await db.Workflows
            .Include(w => w.Statuses)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.Guards)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.Actions)
            .FirstOrDefaultAsync(w => w.Id == project.ActiveWorkflowId, cancellationToken)
            ?? throw new NotFoundException("Workflow", project.ActiveWorkflowId);

        // Find the valid transition
        var transition = workflow.GetTransition(ticket.StatusId, request.ToStatusId)
            ?? throw new WorkflowTransitionException(ticket.StatusId, request.ToStatusId,
                "No such transition exists in the workflow.");

        // Enforce comment requirement
        if (transition.RequiresComment && string.IsNullOrWhiteSpace(request.Comment))
            throw new ValidationException($"A comment is required when transitioning via '{transition.Name}'.");

        // Run guard validations
        var validationResult = await workflowEngine.ValidateTransitionAsync(
            ticket, transition, currentUser.UserId, request.Comment, cancellationToken);

        if (!validationResult.IsValid)
            throw new DomainException($"Transition blocked: {string.Join("; ", validationResult.Errors)}");

        // Resolve the target status
        var toStatus = workflow.Statuses.FirstOrDefault(s => s.Id == request.ToStatusId)
            ?? throw new NotFoundException("WorkflowStatus", request.ToStatusId);

        // Apply transition to ticket
        ticket.TransitionTo(
            request.ToStatusId,
            toStatus.IsFinal ? request.ResolutionId : null,
            toStatus.IsFinal ? dateTime.UtcNow : null,
            currentUser.UserId);

        // Append transition comment if provided
        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            var html = markdownService.RenderToHtml(request.Comment);
            var comment = Domain.Entities.Content.Comment.Create(
                ticket.Id, null, currentUser.UserId, request.Comment, html);
            db.Comments.Add(comment);
        }

        // Record activity
        db.ActivityLogs.Add(Domain.Entities.Audit.ActivityLog.Create(
            ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.StatusChanged, "Ticket", ticket.Id.ToString(),
            "StatusId", ticket.StatusId.ToString(), request.ToStatusId.ToString()));

        // Execute post-transition actions (notifications, webhooks, etc.)
        await workflowEngine.ExecuteActionsAsync(ticket, transition, currentUser.UserId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }
}
