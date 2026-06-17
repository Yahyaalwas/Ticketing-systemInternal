using MediatR;

namespace ITS.Application.Features.Ai.Queries.GetRiskAnalysis;

public sealed record GetRiskAnalysisQuery(
    Guid? ProjectId = null,
    Guid? TicketId = null
) : IRequest<RiskAnalysisDto>;

public sealed record RiskAnalysisDto(
    IReadOnlyList<TicketRisk> Risks,
    int TotalRiskScore,
    string RiskLevel,
    DateTimeOffset AnalyzedAt
);

public sealed record TicketRisk(
    Guid TicketId,
    string TicketKey,
    string Title,
    string RiskType,
    string Description,
    string Severity
);
