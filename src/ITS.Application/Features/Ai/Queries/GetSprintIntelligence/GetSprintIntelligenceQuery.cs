using MediatR;

namespace ITS.Application.Features.Ai.Queries.GetSprintIntelligence;

public sealed record GetSprintIntelligenceQuery(Guid ProjectId) : IRequest<SprintIntelligenceDto>;

public sealed record SprintIntelligenceDto(
    IReadOnlyList<RiskTicket> AtRiskTickets,
    IReadOnlyList<RiskTicket> AgingTickets,
    IReadOnlyList<WorkloadItem> WorkloadImbalance,
    IReadOnlyList<string> SlaBreachWarnings,
    IReadOnlyList<string> RecommendedActions,
    DateTimeOffset GeneratedAt
);

public sealed record RiskTicket(Guid Id, string TicketKey, string Title, string? AssigneeName, string RiskReason, string Severity);
public sealed record WorkloadItem(string AssigneeName, int TicketCount, string Assessment);
