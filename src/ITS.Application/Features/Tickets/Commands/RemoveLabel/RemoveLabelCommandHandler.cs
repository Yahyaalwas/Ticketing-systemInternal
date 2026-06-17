using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.RemoveLabel;

public sealed class RemoveLabelCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<RemoveLabelCommand>
{
    public async Task Handle(RemoveLabelCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .Include(t => t.Labels)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to edit this ticket.");

        var ticketLabel = ticket.Labels.FirstOrDefault(l => l.LabelId == request.LabelId)
            ?? throw new NotFoundException("TicketLabel", request.LabelId);

        db.TicketLabels.Remove(ticketLabel);

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.LabelRemoved, "Ticket", ticket.Id.ToString(),
            "LabelId", request.LabelId.ToString(), null));

        await db.SaveChangesAsync(cancellationToken);
    }
}
