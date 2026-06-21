using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Queries.GetSimilarTickets;

public sealed class GetSimilarTicketsQueryHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiCacheService cache,
    IAiAuditService audit,
    IAiRateLimiter rateLimiter,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSimilarTicketsQuery, SimilarTicketsDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(2);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<SimilarTicketsDto> Handle(GetSimilarTicketsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "similar-tickets", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        var cacheKey = $"ai:similar:{request.TicketId}";
        var cached = await cache.GetAsync(cacheKey, ct);
        if (cached is not null)
        {
            var hit = JsonSerializer.Deserialize<SimilarTicketsDto>(cached, JsonOpts);
            if (hit is not null) return hit;
        }

        var ticketRaw = await db.Tickets.AsNoTracking()
            .Where(t => t.Id == request.TicketId)
            .Select(t => new { t.Title, t.Description, t.IssueTypeId, t.ProjectId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        var issueTypeName = await db.IssueTypes.AsNoTracking()
            .Where(it => it.Id == ticketRaw.IssueTypeId)
            .Select(it => it.Name)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        var project = await db.Projects.AsNoTracking()
            .Where(p => p.Id == ticketRaw.ProjectId)
            .Select(p => new { p.ProjectKey })
            .FirstOrDefaultAsync(ct);

        var ticket = new { ticketRaw.Title, ticketRaw.Description, IssueType = issueTypeName, ticketRaw.ProjectId };

        var candidatesRaw = await db.Tickets.AsNoTracking()
            .Where(t => t.Id != request.TicketId && t.ProjectId == ticket.ProjectId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Take(40)
            .Select(t => new { t.Id, t.TicketNumber, t.Title, t.StatusId, t.ResolutionId })
            .ToListAsync(ct);

        if (candidatesRaw.Count == 0)
            return new SimilarTicketsDto(request.TicketId, [], [], []);

        // Bulk-load statuses and resolutions
        var statusIds = candidatesRaw.Select(c => c.StatusId).Distinct().ToList();
        var resolutionIds = candidatesRaw.Where(c => c.ResolutionId.HasValue).Select(c => c.ResolutionId!.Value).Distinct().ToList();

        var statusMap = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => statusIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var resolutionMap = await db.Resolutions.AsNoTracking()
            .Where(r => resolutionIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, ct);

        var projectKey = project?.ProjectKey ?? "";

        var candidates = candidatesRaw.Select(c => new
        {
            c.Id,
            TicketKey = $"{projectKey}-{c.TicketNumber}",
            c.Title,
            Status = statusMap.GetValueOrDefault(c.StatusId, "Unknown"),
            Resolution = c.ResolutionId.HasValue ? resolutionMap.GetValueOrDefault(c.ResolutionId.Value) : null
        }).ToList();

        var candidateList = string.Join("\n", candidates.Select((c, i) =>
            $"{i + 1}. [{c.TicketKey}] {c.Title} (Status: {c.Status}, Resolution: {c.Resolution ?? "N/A"})"));

        var userPrompt = $"""
            TARGET TICKET: {ticket.Title}
            DESCRIPTION: {ticket.Description ?? "(none)"}
            TYPE: {ticket.IssueType}

            CANDIDATE TICKETS:
            {candidateList}
            """;

        const string systemPrompt = """
            Identify related and similar tickets. Return ONLY valid JSON:
            {
              "related": [{ "index": 1, "similarityPercent": 80, "reason": "Same component" }],
              "similarIncidents": [{ "index": 2, "similarityPercent": 90, "reason": "Identical root cause" }],
              "previousResolutions": ["Resolution approach 1", "Resolution approach 2"]
            }
            Only include tickets with similarity >= 55%.
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, userPrompt, MaxTokens: 800, Temperature: 0.2f,
                CorrelationId: request.TicketId.ToString()), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("similar-tickets", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false,
            request.TicketId.ToString(), start), ct);

        SimilarJson parsed;
        try { parsed = JsonSerializer.Deserialize<SimilarJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { parsed = new SimilarJson(); }

        SimilarTicketItem ToItem(SimilarEntry e)
        {
            var idx = e.Index - 1;
            if (idx < 0 || idx >= candidates.Count) return null!;
            var c = candidates[idx];
            return new SimilarTicketItem(c.Id, c.TicketKey, c.Title, c.Status,
                Math.Clamp(e.SimilarityPercent, 0, 100), c.Resolution);
        }

        var related = (parsed.Related ?? []).Select(ToItem).Where(x => x is not null).ToList();
        var incidents = (parsed.SimilarIncidents ?? []).Select(ToItem).Where(x => x is not null).ToList();

        var dto = new SimilarTicketsDto(request.TicketId, related, incidents, parsed.PreviousResolutions ?? []);
        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(dto), CacheTtl, ct);
        return dto;
    }

    private sealed class SimilarJson { public List<SimilarEntry>? Related { get; set; } public List<SimilarEntry>? SimilarIncidents { get; set; } public List<string>? PreviousResolutions { get; set; } }
    private sealed class SimilarEntry { public int Index { get; set; } public int SimilarityPercent { get; set; } public string? Reason { get; set; } }
}
