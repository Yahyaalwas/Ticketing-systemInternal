using MediatR;

namespace ITS.Application.Features.Tickets.Commands.AddWatcher;

public sealed record AddWatcherCommand(Guid TicketId, Guid UserId) : IRequest;
