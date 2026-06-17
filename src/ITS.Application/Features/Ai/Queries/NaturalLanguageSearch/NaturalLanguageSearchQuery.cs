using MediatR;

namespace ITS.Application.Features.Ai.Queries.NaturalLanguageSearch;

public sealed record NaturalLanguageSearchQuery(
    string UserQuery,
    Guid? ProjectId = null
) : IRequest<NaturalLanguageSearchResult>;

public sealed record NaturalLanguageSearchResult(
    string Interpretation,
    ParsedSearchFilters Filters,
    IReadOnlyList<TicketSearchHit> Tickets
);

public sealed record ParsedSearchFilters(
    string? AssigneeName,
    string? PriorityName,
    string? StatusName,
    string? IssueTypeName,
    string? DepartmentName,
    string? LabelName,
    bool? OverdueOnly,
    bool? UnassignedOnly,
    string? FreeTextSearch
);

public sealed record TicketSearchHit(
    Guid Id,
    string TicketKey,
    string Title,
    string StatusName,
    string? PriorityName,
    string? AssigneeName,
    DateOnly? DueDate,
    bool IsOverdue
);
