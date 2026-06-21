using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Queries.GetSprintIntelligence;

public sealed class GetSprintIntelligenceQueryHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiCacheService cache,
    IAiAuditService audit,
    IAiRateLimiter rateLimiter,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSprintIntelligenceQuery, SprintIntelligenceDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<SprintIntelligenceDto> Handle(GetSprintIntelligenceQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "sprint-intelligence", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        var cacheKey = $"ai:sprint:{request.ProjectId}";
        var cached = await cache.GetAsync(cacheKey, ct);
        if (cached is not null)
        {
            var hit = JsonSerializer.Deserialize<SprintIntelligenceDto>(cached, JsonOpts);
            if (hit is not null) return hit;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Get IDs of non-Done statuses for filtering
        var doneStatusIds = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => s.Category == ITS.Domain.Enums.StatusCategory.Done)
            .Select(s => s.Id).ToListAsync(ct);

        var ticketsRaw = await db.Tickets.AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId && !t.IsDeleted && !doneStatusIds.Contains(t.StatusId))
            .Select(t => new
            {
                t.Id, t.TicketNumber, t.Title,
                t.StatusId, t.PriorityId, t.AssigneeUserId,
                t.DueDate, t.SlaBreachAt,
                t.CreatedAt, t.UpdatedAt
            })
            .ToListAsync(ct);

        if (ticketsRaw.Count == 0)
            return new SprintIntelligenceDto([], [], [], [], ["No active tickets found."], DateTimeOffset.UtcNow);

        // Bulk-load reference data
        var sprintStatusIds = ticketsRaw.Select(t => t.StatusId).Distinct().ToList();
        var sprintPriorityIds = ticketsRaw.Where(t => t.PriorityId.HasValue).Select(t => t.PriorityId!.Value).Distinct().ToList();
        var sprintAssigneeIds = ticketsRaw.Where(t => t.AssigneeUserId.HasValue).Select(t => t.AssigneeUserId!.Value).Distinct().ToList();

        var sprintProject = await db.Projects.AsNoTracking()
            .Where(p => p.Id == request.ProjectId).Select(p => new { p.ProjectKey }).FirstOrDefaultAsync(ct);
        var sprintProjectKey = sprintProject?.ProjectKey ?? "";

        var sprintStatusMap = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => sprintStatusIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var sprintPriorityMap = await db.Priorities.AsNoTracking()
            .Where(p => sprintPriorityIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var sprintAssigneeMap = await db.Users.AsNoTracking()
            .Where(u => sprintAssigneeIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var tickets = ticketsRaw.Select(t => new
        {
            t.Id,
            TicketKey = $"{sprintProjectKey}-{t.TicketNumber}",
            t.Title,
            Status = sprintStatusMap.GetValueOrDefault(t.StatusId, "Unknown"),
            Priority = t.PriorityId.HasValue ? sprintPriorityMap.GetValueOrDefault(t.PriorityId.Value, "None") : "None",
            Assignee = t.AssigneeUserId.HasValue ? sprintAssigneeMap.GetValueOrDefault(t.AssigneeUserId.Value, "Unassigned") : "Unassigned",
            t.DueDate, t.SlaBreachAt,
            IsSlaBreached = t.SlaBreachAt.HasValue && t.SlaBreachAt.Value < now,
            t.CreatedAt, t.UpdatedAt
        }).ToList();

        var ticketSummary = string.Join("\n", tickets.Select(t =>
            $"- [{t.TicketKey}] {t.Title} | Priority:{t.Priority} | Assignee:{t.Assignee} | " +
            $"Due:{t.DueDate?.ToString("yyyy-MM-dd") ?? "N/A"} | Updated:{t.UpdatedAt:yyyy-MM-dd} | " +
            $"Reopened:{0}x | SLABreached:{t.IsSlaBreached}"));

        var workloadSummary = tickets
            .GroupBy(t => t.Assignee)
            .Select(g => $"{g.Key}: {g.Count()} tickets")
            .ToList();

        var userPrompt = $"""
            PROJECT ACTIVE TICKETS ({tickets.Count} total):
            {ticketSummary}

            WORKLOAD:
            {string.Join("\n", workloadSummary)}

            TODAY: {today:yyyy-MM-dd}
            """;

        const string systemPrompt = """
            You are an agile sprint analyst. Analyze the active tickets and return ONLY valid JSON:
            {
              "atRiskTickets": [
                { "ticketKey": "IT-123", "riskReason": "No assignee + due tomorrow", "severity": "High|Medium|Low" }
              ],
              "agingTickets": [
                { "ticketKey": "IT-456", "riskReason": "No update for 14 days", "severity": "Medium" }
              ],
              "workloadImbalance": [
                { "assigneeName": "John", "assessment": "Overloaded with 8 tickets" }
              ],
              "slaBreachWarnings": ["IT-789 will breach SLA in 2 hours"],
              "recommendedActions": ["Reassign IT-456 to available team member", "Escalate IT-789 to manager"]
            }
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, userPrompt, MaxTokens: 1200, Temperature: 0.3f,
                CorrelationId: request.ProjectId.ToString()), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("sprint-intelligence", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false,
            request.ProjectId.ToString(), start), ct);

        SprintJson parsed;
        try { parsed = JsonSerializer.Deserialize<SprintJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { parsed = new SprintJson(); }

        var ticketDict = tickets.ToDictionary(t => t.TicketKey);

        RiskTicket ToRisk(RiskEntry e)
        {
            ticketDict.TryGetValue(e.TicketKey ?? "", out var t);
            return new RiskTicket(t?.Id ?? Guid.Empty, e.TicketKey ?? "", t?.Title ?? e.TicketKey ?? "",
                t?.Assignee, e.RiskReason ?? "", e.Severity ?? "Medium");
        }

        var dto = new SprintIntelligenceDto(
            (parsed.AtRiskTickets ?? []).Select(ToRisk).ToList(),
            (parsed.AgingTickets ?? []).Select(ToRisk).ToList(),
            (parsed.WorkloadImbalance ?? []).Select(w =>
                new WorkloadItem(w.AssigneeName ?? "", 0, w.Assessment ?? "")).ToList(),
            parsed.SlaBreachWarnings ?? [],
            parsed.RecommendedActions ?? [],
            DateTimeOffset.UtcNow);

        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(dto), CacheTtl, ct);
        return dto;
    }

    private sealed class SprintJson
    {
        public List<RiskEntry>? AtRiskTickets { get; set; }
        public List<RiskEntry>? AgingTickets { get; set; }
        public List<WorkloadEntry>? WorkloadImbalance { get; set; }
        public List<string>? SlaBreachWarnings { get; set; }
        public List<string>? RecommendedActions { get; set; }
    }
    private sealed class RiskEntry { public string? TicketKey { get; set; } public string? RiskReason { get; set; } public string? Severity { get; set; } }
    private sealed class WorkloadEntry { public string? AssigneeName { get; set; } public string? Assessment { get; set; } }
}
