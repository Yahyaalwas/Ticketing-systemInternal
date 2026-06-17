using MediatR;

namespace ITS.Application.Features.Tickets.Commands.UpdateTicket;

public sealed record UpdateTicketCommand(
    Guid TicketId,
    string Title,
    string? Description,
    int IssueTypeId,
    int? PriorityId,
    Guid? AssigneeUserId,
    DateOnly? DueDate,
    decimal? StoryPoints,
    decimal? EstimatedHours,
    byte[] RowVersion
) : IRequest;
