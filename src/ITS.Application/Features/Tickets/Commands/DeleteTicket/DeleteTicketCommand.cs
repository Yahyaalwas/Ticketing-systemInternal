using MediatR;

namespace ITS.Application.Features.Tickets.Commands.DeleteTicket;

public sealed record DeleteTicketCommand(Guid TicketId) : IRequest;
