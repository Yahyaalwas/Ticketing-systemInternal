using MediatR;

namespace ITS.Application.Features.Tickets.Commands.ChangePriority;

public sealed record ChangePriorityCommand(Guid TicketId, int? PriorityId, byte[] RowVersion) : IRequest;
