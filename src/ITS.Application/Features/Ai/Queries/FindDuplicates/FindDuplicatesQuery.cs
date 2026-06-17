using MediatR;

namespace ITS.Application.Features.Ai.Queries.FindDuplicates;

public sealed record FindDuplicatesQuery(
    string Title,
    string? Description,
    Guid ProjectId
) : IRequest<DuplicateDetectionResult>;

public sealed record DuplicateDetectionResult(
    bool HasPotentialDuplicates,
    IReadOnlyList<DuplicateCandidate> Candidates
);

public sealed record DuplicateCandidate(
    Guid TicketId,
    string TicketKey,
    string Title,
    string StatusName,
    int SimilarityPercent,
    string SimilarityReason
);
