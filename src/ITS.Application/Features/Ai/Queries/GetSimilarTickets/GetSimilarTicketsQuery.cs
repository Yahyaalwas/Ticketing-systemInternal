using MediatR;

namespace ITS.Application.Features.Ai.Queries.GetSimilarTickets;

public sealed record GetSimilarTicketsQuery(Guid TicketId) : IRequest<SimilarTicketsDto>;

public sealed record SimilarTicketsDto(
    Guid TicketId,
    IReadOnlyList<SimilarTicketItem> Related,
    IReadOnlyList<SimilarTicketItem> SimilarIncidents,
    IReadOnlyList<string> PreviousResolutions
);

public sealed record SimilarTicketItem(
    Guid Id,
    string TicketKey,
    string Title,
    string StatusName,
    int SimilarityPercent,
    string? Resolution
);
