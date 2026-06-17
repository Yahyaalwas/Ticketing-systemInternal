using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.UpdateTicket;

public sealed class UpdateTicketCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IMarkdownService markdownService,
    IProjectAuthorizationService authz)
    : IRequestHandler<UpdateTicketCommand>
{
    public async Task Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!ticket.RowVersion.SequenceEqual(request.RowVersion))
            throw new ConflictException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to edit this ticket.");

        // Validate issue type belongs to project
        var issueType = await db.IssueTypes.FindAsync([request.IssueTypeId], cancellationToken)
            ?? throw new NotFoundException("IssueType", request.IssueTypeId);
        if (issueType.ProjectId != ticket.ProjectId)
            throw new ForbiddenAccessException("Issue type does not belong to this project.");

        // Track changes for activity log
        if (ticket.Title != request.Title)
        {
            db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
                ActivityType.FieldUpdated, "Ticket", ticket.Id.ToString(), "Title", ticket.Title, request.Title));
        }

        if (ticket.PriorityId != request.PriorityId)
        {
            db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
                ActivityType.PriorityChanged, "Ticket", ticket.Id.ToString(),
                "PriorityId", ticket.PriorityId?.ToString(), request.PriorityId?.ToString()));
        }

        if (ticket.AssigneeUserId != request.AssigneeUserId)
        {
            db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
                ActivityType.AssigneeChanged, "Ticket", ticket.Id.ToString(),
                "AssigneeUserId", ticket.AssigneeUserId?.ToString(), request.AssigneeUserId?.ToString()));
        }

        if (ticket.DueDate != request.DueDate)
        {
            db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
                ActivityType.FieldUpdated, "Ticket", ticket.Id.ToString(),
                "DueDate", ticket.DueDate?.ToString(), request.DueDate?.ToString()));
        }

        ticket.UpdateTitle(request.Title, currentUser.UserId);

        var html = !string.IsNullOrWhiteSpace(request.Description)
            ? markdownService.RenderToHtml(request.Description)
            : null;
        ticket.UpdateDescription(request.Description, html);

        ticket.Assign(request.AssigneeUserId, currentUser.UserId);
        ticket.ChangePriority(request.PriorityId);
        ticket.SetDueDate(request.DueDate);
        ticket.SetStoryPoints(request.StoryPoints);
        ticket.SetEstimatedHours(request.EstimatedHours);

        await db.SaveChangesAsync(cancellationToken);
    }
}
