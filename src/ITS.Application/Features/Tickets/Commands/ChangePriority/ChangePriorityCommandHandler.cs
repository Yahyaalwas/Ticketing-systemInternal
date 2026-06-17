using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.ChangePriority;

public sealed class ChangePriorityCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IDateTimeService dateTime)
    : IRequestHandler<ChangePriorityCommand>
{
    public async Task Handle(ChangePriorityCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!ticket.RowVersion.SequenceEqual(request.RowVersion))
            throw new ConflictException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to edit this ticket.");

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.PriorityChanged, "Ticket", ticket.Id.ToString(),
            "PriorityId", ticket.PriorityId?.ToString(), request.PriorityId?.ToString()));

        ticket.ChangePriority(request.PriorityId);

        // Recalculate SLA breach time if priority changed
        if (request.PriorityId.HasValue)
        {
            var priority = await db.Priorities.FindAsync([request.PriorityId.Value], cancellationToken);
            if (priority?.SlaTargetHours.HasValue == true)
                ticket.SetSlaBreachAt(dateTime.UtcNow.AddHours(priority.SlaTargetHours.Value));
        }
        else
        {
            ticket.SetSlaBreachAt(null);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
