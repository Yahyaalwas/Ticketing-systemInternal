using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.DeleteTicket;

public sealed class DeleteTicketCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime,
    IProjectAuthorizationService authz)
    : IRequestHandler<DeleteTicketCommand>
{
    public async Task Handle(DeleteTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Tickets.Ticket), request.TicketId);

        if (!await authz.CanDeleteTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to delete tickets in this project.");

        ticket.SoftDelete(currentUser.UserId, dateTime.UtcNow);

        db.ActivityLogs.Add(Domain.Entities.Audit.ActivityLog.Create(
            ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.TicketDeleted, "Ticket", ticket.Id.ToString(),
            null, null, null));

        await db.SaveChangesAsync(cancellationToken);
    }
}
