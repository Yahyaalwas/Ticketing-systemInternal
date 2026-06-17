using MediatR;

namespace ITS.Application.Features.Tickets.Commands.ChangeDueDate;

public sealed record ChangeDueDateCommand(Guid TicketId, DateOnly? DueDate, byte[] RowVersion) : IRequest;
