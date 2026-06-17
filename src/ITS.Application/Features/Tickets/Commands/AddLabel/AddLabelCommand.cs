using MediatR;

namespace ITS.Application.Features.Tickets.Commands.AddLabel;

public sealed record AddLabelCommand(Guid TicketId, int LabelId) : IRequest;
