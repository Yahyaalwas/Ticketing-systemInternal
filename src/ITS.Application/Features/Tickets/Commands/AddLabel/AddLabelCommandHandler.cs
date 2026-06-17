using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Entities.Tickets;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Tickets.Commands.AddLabel;

public sealed class AddLabelCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<AddLabelCommand>
{
    public async Task Handle(AddLabelCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .Include(t => t.Labels)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to edit this ticket.");

        var label = await db.Labels.FindAsync([request.LabelId], cancellationToken)
            ?? throw new NotFoundException("Label", request.LabelId);

        if (ticket.Labels.Any(l => l.LabelId == request.LabelId))
            throw new ConflictException("Label is already added to this ticket.");

        ticket.Labels.Add(TicketLabel.Create(ticket.Id, request.LabelId));

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.LabelAdded, "Ticket", ticket.Id.ToString(),
            "LabelId", null, request.LabelId.ToString()));

        await db.SaveChangesAsync(cancellationToken);
    }
}
