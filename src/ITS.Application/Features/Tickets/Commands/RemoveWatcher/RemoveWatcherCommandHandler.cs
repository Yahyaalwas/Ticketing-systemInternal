using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.RemoveWatcher;

public sealed class RemoveWatcherCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<RemoveWatcherCommand>
{
    public async Task Handle(RemoveWatcherCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .Include(t => t.Watchers)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to manage watchers on this ticket.");

        ticket.RemoveWatcher(request.UserId);

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.WatcherRemoved, "Ticket", ticket.Id.ToString(),
            "UserId", request.UserId.ToString(), null));

        await db.SaveChangesAsync(cancellationToken);
    }
}
