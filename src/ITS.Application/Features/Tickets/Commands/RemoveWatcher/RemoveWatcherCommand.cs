using MediatR;

namespace ITS.Application.Features.Tickets.Commands.RemoveWatcher;

public sealed record RemoveWatcherCommand(Guid TicketId, Guid UserId) : IRequest;
