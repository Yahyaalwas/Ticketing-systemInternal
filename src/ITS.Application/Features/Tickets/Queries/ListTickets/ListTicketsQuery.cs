using MediatR;

namespace ITS.Application.Features.Tickets.Queries.ListTickets;

public sealed record ListTicketsQuery(
    Guid ProjectId,
    int PageNumber = 1,
    int PageSize = 25,
    string? Search = null,
    int? StatusId = null,
    int? PriorityId = null,
    int? IssueTypeId = null,
    Guid? AssigneeUserId = null,
    int? LabelId = null,
    bool? IsOverdue = null,
    string? SortBy = null,
    bool SortDescending = false
) : IRequest<TicketListResult>;

public sealed record TicketListResult(
    IReadOnlyList<TicketSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int TotalPages);

public sealed record TicketSummaryDto(
    Guid Id,
    string TicketKey,
    int TicketNumber,
    string Title,
    string IssueTypeName,
    string? IssueTypeIconUrl,
    string StatusName,
    string StatusCategory,
    string? StatusColor,
    string? PriorityName,
    string? PriorityColor,
    Guid? AssigneeUserId,
    string? AssigneeName,
    string? AssigneeAvatarUrl,
    DateOnly? DueDate,
    DateTime? SlaBreachAt,
    bool IsSlaBreached,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion);
