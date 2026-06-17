using MediatR;

namespace ITS.Application.Features.Ai.Commands.SummarizeTicket;

public sealed record SummarizeTicketCommand(
    Guid TicketId,
    bool ForceRefresh = false
) : IRequest<TicketAiSummaryDto>;

public sealed record TicketAiSummaryDto(
    Guid TicketId,
    string ExecutiveSummary,
    string TechnicalSummary,
    string SimpleExplanation,
    IReadOnlyList<string> KeyDecisions,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> ActionItems,
    int CompletionConfidencePercent,
    bool WasFromCache,
    DateTimeOffset GeneratedAt
);
