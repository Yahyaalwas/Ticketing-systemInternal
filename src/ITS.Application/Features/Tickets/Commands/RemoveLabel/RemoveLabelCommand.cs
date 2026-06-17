using MediatR;

namespace ITS.Application.Features.Tickets.Commands.RemoveLabel;

public sealed record RemoveLabelCommand(Guid TicketId, int LabelId) : IRequest;
