using MediatR;

namespace ITS.Application.Features.Tickets.Commands.CreateTicket;

public sealed record CreateTicketCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    int IssueTypeId,
    int? PriorityId,
    Guid? AssigneeUserId,
    Guid? ParentTicketId,
    Guid? EpicTicketId,
    DateOnly? DueDate,
    decimal? StoryPoints,
    IReadOnlyList<int>? LabelIds,
    IReadOnlyDictionary<int, string>? CustomFieldValues
) : IRequest<CreateTicketResponse>;

public sealed record CreateTicketResponse(
    Guid TicketId,
    string TicketKey,
    int TicketNumber);
