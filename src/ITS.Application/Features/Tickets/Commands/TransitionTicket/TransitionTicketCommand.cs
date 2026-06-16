using MediatR;

namespace ITS.Application.Features.Tickets.Commands.TransitionTicket;

public sealed record TransitionTicketCommand(
    Guid TicketId,
    int ToStatusId,
    string? Comment,
    int? ResolutionId,
    byte[] RowVersion
) : IRequest;
