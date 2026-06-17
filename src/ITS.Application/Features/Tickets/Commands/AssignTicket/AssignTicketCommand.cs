using MediatR;

namespace ITS.Application.Features.Tickets.Commands.AssignTicket;

public sealed record AssignTicketCommand(Guid TicketId, Guid? AssigneeUserId, byte[] RowVersion) : IRequest;
