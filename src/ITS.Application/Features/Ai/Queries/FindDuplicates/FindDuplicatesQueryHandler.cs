using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Queries.FindDuplicates;

public sealed class FindDuplicatesQueryHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiRateLimiter rateLimiter,
    IAiAuditService audit,
    IAiDataMasker masker,
    ICurrentUserService currentUser)
    : IRequestHandler<FindDuplicatesQuery, DuplicateDetectionResult>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<DuplicateDetectionResult> Handle(FindDuplicatesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "find-duplicates", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        // Fetch recent tickets from the same project for comparison
        var project = await db.Projects.AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new { p.ProjectKey })
            .FirstOrDefaultAsync(ct);

        var projectKey = project?.ProjectKey ?? "";

        var candidatesRaw = await db.Tickets
            .AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new { t.Id, t.TicketNumber, t.Title, t.StatusId })
            .ToListAsync(ct);

        if (candidatesRaw.Count == 0)
            return new DuplicateDetectionResult(false, []);

        var statusIds = candidatesRaw.Select(c => c.StatusId).Distinct().ToList();
        var statusMap = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => statusIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var candidates = candidatesRaw.Select(c => new
        {
            c.Id,
            TicketKey = $"{projectKey}-{c.TicketNumber}",
            c.Title,
            Status = statusMap.GetValueOrDefault(c.StatusId, "Unknown")
        }).ToList();

        var candidateList = string.Join("\n", candidates.Select((c, i) =>
            $"{i + 1}. [{c.TicketKey}] {c.Title} (Status: {c.Status})"));

        var rawPrompt = $"""
            NEW TICKET:
            Title: {request.Title}
            Description: {request.Description ?? "(none)"}

            EXISTING TICKETS:
            {candidateList}
            """;

        const string systemPrompt = """
            You are a duplicate detection expert. Compare the new ticket against existing tickets and return ONLY valid JSON:
            {
              "duplicates": [
                {
                  "index": 1,
                  "similarityPercent": 85,
                  "reason": "Same issue with payment gateway timeout"
                }
              ]
            }
            Only include tickets with similarity >= 60%. Return empty array if no duplicates found.
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, masker.Mask(rawPrompt), MaxTokens: 600, Temperature: 0.1f), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("find-duplicates", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false, null, start), ct);

        DuplicateJson parsed;
        try { parsed = JsonSerializer.Deserialize<DuplicateJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { return new DuplicateDetectionResult(false, []); }

        var results = new List<DuplicateCandidate>();
        foreach (var d in parsed.Duplicates ?? [])
        {
            var idx = d.Index - 1;
            if (idx < 0 || idx >= candidates.Count) continue;
            var c = candidates[idx];
            results.Add(new DuplicateCandidate(c.Id, c.TicketKey, c.Title, c.Status,
                Math.Clamp(d.SimilarityPercent, 0, 100), d.Reason ?? "Similar content"));
        }

        results = [.. results.OrderByDescending(r => r.SimilarityPercent)];
        return new DuplicateDetectionResult(results.Count > 0, results);
    }

    private sealed class DuplicateJson { public List<DuplicateEntry>? Duplicates { get; set; } }
    private sealed class DuplicateEntry { public int Index { get; set; } public int SimilarityPercent { get; set; } public string? Reason { get; set; } }
}
