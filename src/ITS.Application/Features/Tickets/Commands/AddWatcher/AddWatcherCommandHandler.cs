using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.AddWatcher;

public sealed class AddWatcherCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<AddWatcherCommand>
{
    public async Task Handle(AddWatcherCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .Include(t => t.Watchers)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to watch this ticket.");

        _ = await db.Users.FindAsync([request.UserId], cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        ticket.AddWatcher(request.UserId);

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.WatcherAdded, "Ticket", ticket.Id.ToString(),
            "UserId", null, request.UserId.ToString()));

        await db.SaveChangesAsync(cancellationToken);
    }
}
