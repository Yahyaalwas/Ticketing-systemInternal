using MediatR;

namespace ITS.Application.Features.Tickets.Queries.GetKanbanBoard;

public sealed record GetKanbanBoardQuery(
    Guid ProjectId,
    Guid? AssigneeUserId = null,
    int? PriorityId = null,
    int? LabelId = null,
    Guid? EpicTicketId = null,
    int? IssueTypeId = null,
    string? Search = null
) : IRequest<KanbanBoardDto>;

public sealed record KanbanBoardDto(
    Guid ProjectId,
    string ProjectKey,
    string ProjectName,
    IReadOnlyList<KanbanColumnDto> Columns);

public sealed record KanbanColumnDto(
    int StatusId,
    string StatusName,
    string StatusCategory,
    string? StatusColor,
    int DisplayOrder,
    int? WipLimit,
    IReadOnlyList<KanbanTicketDto> Tickets);

public sealed record KanbanTicketDto(
    Guid Id,
    int TicketNumber,
    string TicketKey,
    string Title,
    string IssueTypeName,
    string? IssueTypeIconUrl,
    string? PriorityName,
    string? PriorityColor,
    Guid? AssigneeUserId,
    string? AssigneeName,
    string? AssigneeAvatarUrl,
    DateOnly? DueDate,
    decimal? StoryPoints,
    DateTime? SlaBreachAt,
    bool IsSlaBreached,
    IReadOnlyList<string> LabelNames,
    int CommentCount,
    int AttachmentCount,
    byte[] RowVersion);
