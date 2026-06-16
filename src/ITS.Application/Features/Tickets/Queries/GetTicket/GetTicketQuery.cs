using MediatR;

namespace ITS.Application.Features.Tickets.Queries.GetTicket;

public sealed record GetTicketQuery(Guid TicketId) : IRequest<TicketDetailDto>;

public sealed record TicketDetailDto(
    Guid Id,
    Guid ProjectId,
    string ProjectKey,
    int TicketNumber,
    string TicketKey,
    string Title,
    string? Description,
    string? DescriptionHtml,
    string IssueTypeName,
    string StatusName,
    string StatusCategory,
    string? StatusColor,
    string? PriorityName,
    string? PriorityColor,
    Guid? AssigneeUserId,
    string? AssigneeName,
    string? AssigneeAvatarUrl,
    Guid ReporterUserId,
    string ReporterName,
    Guid? ParentTicketId,
    string? ParentTicketKey,
    Guid? EpicTicketId,
    string? EpicTicketKey,
    DateOnly? DueDate,
    decimal? StoryPoints,
    decimal? EstimatedHours,
    decimal? ActualHours,
    string? ResolutionName,
    DateTime? ResolvedAt,
    DateTime? SlaBreachAt,
    bool IsDeleted,
    DateTime CreatedAt,
    string CreatedByName,
    DateTime UpdatedAt,
    byte[] RowVersion,
    IReadOnlyList<LabelDto> Labels,
    IReadOnlyList<TicketLinkDto> Links,
    IReadOnlyList<TicketWatcherDto> Watchers,
    int CommentCount,
    int AttachmentCount);

public sealed record LabelDto(int Id, string Name, string? Color);

public sealed record TicketLinkDto(
    Guid LinkId,
    Guid LinkedTicketId,
    string LinkedTicketKey,
    string LinkedTicketTitle,
    string LinkDescription);

public sealed record TicketWatcherDto(Guid UserId, string DisplayName);
