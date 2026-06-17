using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Queries.GetExecutiveReport;

public sealed class GetExecutiveReportQueryHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiCacheService cache,
    IAiAuditService audit,
    IAiRateLimiter rateLimiter,
    ICurrentUserService currentUser)
    : IRequestHandler<GetExecutiveReportQuery, ExecutiveReportDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(4);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<ExecutiveReportDto> Handle(GetExecutiveReportQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "executive-report", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        var cacheKey = $"ai:exec-report:{request.Period}:{request.ProjectId}:{request.DepartmentId}";

        if (!request.ForceRefresh)
        {
            var cached = await cache.GetAsync(cacheKey, ct);
            if (cached is not null)
            {
                var hit = JsonSerializer.Deserialize<ExecutiveReportDto>(cached, JsonOpts);
                if (hit is not null) return hit with { WasFromCache = true };
            }
        }

        var now = DateTime.UtcNow;
        var periodStart = request.Period == ReportPeriod.Weekly ? now.AddDays(-7) : now.AddDays(-30);
        var periodLabel = request.Period == ReportPeriod.Weekly ? "Last 7 Days" : "Last 30 Days";

        var ticketQuery = db.Tickets.AsNoTracking().Where(t => !t.IsDeleted);
        if (request.ProjectId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.ProjectId == request.ProjectId.Value);

        var allTickets = await ticketQuery
            .Select(t => new
            {
                t.Id, t.Title, StatusCategory = t.Status!.Category,
                Priority = t.Priority != null ? t.Priority.Name : "None",
                t.CreatedAt, t.ResolvedAt, t.DueDate,
                t.SlaBreachAt, t.IsSlaBreached,
                IssueType = t.IssueType!.Name,
                Assignee = t.Assignee != null ? t.Assignee.DisplayName : "Unassigned"
            })
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(now);
        var totalTickets = allTickets.Count;
        var openTickets = allTickets.Count(t => t.StatusCategory != "Done");
        var resolvedTickets = allTickets.Count(t => t.StatusCategory == "Done");
        var overdueTickets = allTickets.Count(t => t.DueDate.HasValue && t.DueDate < today && t.StatusCategory != "Done");
        var slaBreaches = allTickets.Count(t => t.IsSlaBreached);
        var slaCompliance = totalTickets > 0 ? Math.Round((1.0 - (double)slaBreaches / totalTickets) * 100, 1) : 100.0;

        var resolvedWithTime = allTickets
            .Where(t => t.ResolvedAt.HasValue)
            .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
            .ToList();
        var avgResolutionHours = resolvedWithTime.Count > 0 ? (int)resolvedWithTime.Average() : 0;

        var byIssueType = allTickets
            .GroupBy(t => t.IssueType)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key}: {g.Count()} tickets");

        var metricsContext = $"""
            PERIOD: {periodLabel}
            Total Tickets: {totalTickets}
            Open: {openTickets} | Resolved: {resolvedTickets} | Overdue: {overdueTickets}
            SLA Breaches: {slaBreaches} | SLA Compliance: {slaCompliance}%
            Avg Resolution Time: {avgResolutionHours} hours
            Top Issue Types: {string.Join(", ", byIssueType)}
            """;

        const string systemPrompt = """
            You are an executive IT operations analyst. Generate a comprehensive report from the metrics. Return ONLY valid JSON:
            {
              "narrativeSummary": "Executive narrative 3-5 sentences",
              "teamPerformanceOverview": "Team performance analysis 2-3 sentences",
              "slaHealthReport": "SLA health analysis 2-3 sentences",
              "deliveryRisks": ["risk 1", "risk 2", "risk 3"],
              "resourceBottlenecks": ["bottleneck 1", "bottleneck 2"],
              "topRecurringCategories": ["category 1", "category 2", "category 3"]
            }
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, metricsContext, MaxTokens: 1500, Temperature: 0.4f), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("executive-report", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false, null, start), ct);

        ReportJson parsed;
        try { parsed = JsonSerializer.Deserialize<ReportJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { parsed = new ReportJson { NarrativeSummary = aiResp.Content }; }

        var dto = new ExecutiveReportDto(
            periodLabel,
            parsed.NarrativeSummary ?? "",
            parsed.TeamPerformanceOverview ?? "",
            parsed.SlaHealthReport ?? "",
            parsed.DeliveryRisks ?? [],
            parsed.ResourceBottlenecks ?? [],
            parsed.TopRecurringCategories ?? [],
            new ReportMetrics(totalTickets, openTickets, resolvedTickets, overdueTickets,
                avgResolutionHours, slaBreaches, slaCompliance),
            WasFromCache: false,
            DateTimeOffset.UtcNow);

        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(dto), CacheTtl, ct);
        return dto;
    }

    private sealed class ReportJson
    {
        public string? NarrativeSummary { get; set; }
        public string? TeamPerformanceOverview { get; set; }
        public string? SlaHealthReport { get; set; }
        public List<string>? DeliveryRisks { get; set; }
        public List<string>? ResourceBottlenecks { get; set; }
        public List<string>? TopRecurringCategories { get; set; }
    }
}
