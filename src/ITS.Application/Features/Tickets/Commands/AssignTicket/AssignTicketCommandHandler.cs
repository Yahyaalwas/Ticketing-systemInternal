using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.AssignTicket;

public sealed class AssignTicketCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    INotificationService notifications)
    : IRequestHandler<AssignTicketCommand>
{
    public async Task Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!ticket.RowVersion.SequenceEqual(request.RowVersion))
            throw new ConflictException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to assign this ticket.");

        if (request.AssigneeUserId.HasValue)
        {
            _ = await db.Users.FindAsync([request.AssigneeUserId.Value], cancellationToken)
                ?? throw new NotFoundException("User", request.AssigneeUserId.Value);
        }

        var previousAssignee = ticket.AssigneeUserId;
        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.AssigneeChanged, "Ticket", ticket.Id.ToString(),
            "AssigneeUserId", previousAssignee?.ToString(), request.AssigneeUserId?.ToString()));

        ticket.Assign(request.AssigneeUserId, currentUser.UserId);

        // Notify new assignee
        if (request.AssigneeUserId.HasValue && request.AssigneeUserId != currentUser.UserId)
        {
            await notifications.NotifyAsync(
                request.AssigneeUserId.Value, NotificationType.TicketAssigned,
                "Ticket assigned to you", ticket.Title,
                ticket.Id, ticket.ProjectId, currentUser.UserId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
