using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.ChangeDueDate;

public sealed class ChangeDueDateCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<ChangeDueDateCommand>
{
    public async Task Handle(ChangeDueDateCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!ticket.RowVersion.SequenceEqual(request.RowVersion))
            throw new ConflictException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to edit this ticket.");

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.FieldUpdated, "Ticket", ticket.Id.ToString(),
            "DueDate", ticket.DueDate?.ToString(), request.DueDate?.ToString()));

        ticket.SetDueDate(request.DueDate);
        await db.SaveChangesAsync(cancellationToken);
    }
}
