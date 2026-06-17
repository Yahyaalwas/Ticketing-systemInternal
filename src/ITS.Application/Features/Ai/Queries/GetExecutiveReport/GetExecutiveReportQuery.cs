using MediatR;

namespace ITS.Application.Features.Ai.Queries.GetExecutiveReport;

public sealed record GetExecutiveReportQuery(
    ReportPeriod Period,
    Guid? ProjectId = null,
    Guid? DepartmentId = null,
    bool ForceRefresh = false
) : IRequest<ExecutiveReportDto>;

public enum ReportPeriod { Weekly, Monthly }

public sealed record ExecutiveReportDto(
    string PeriodLabel,
    string NarrativeSummary,
    string TeamPerformanceOverview,
    string SlaHealthReport,
    IReadOnlyList<string> DeliveryRisks,
    IReadOnlyList<string> ResourceBottlenecks,
    IReadOnlyList<string> TopRecurringCategories,
    ReportMetrics Metrics,
    bool WasFromCache,
    DateTimeOffset GeneratedAt
);

public sealed record ReportMetrics(
    int TotalTickets,
    int OpenTickets,
    int ResolvedTickets,
    int OverdueTickets,
    int AverageResolutionHours,
    int SlaBreachCount,
    double SlaCompliancePercent
);
