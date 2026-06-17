using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Commands.SummarizeTicket;

public sealed class SummarizeTicketCommandHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiCacheService cache,
    IAiAuditService audit,
    IAiRateLimiter rateLimiter,
    IAiDataMasker masker,
    ICurrentUserService currentUser)
    : IRequestHandler<SummarizeTicketCommand, TicketAiSummaryDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<TicketAiSummaryDto> Handle(SummarizeTicketCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();

        if (!await rateLimiter.IsAllowedAsync(userId, "summarize-ticket", ct))
            throw new InvalidOperationException("AI rate limit exceeded. Please try again later.");

        var cacheKey = $"ai:summarize:{request.TicketId}";

        if (!request.ForceRefresh)
        {
            var cached = await cache.GetAsync(cacheKey, ct);
            if (cached is not null)
            {
                var hit = JsonSerializer.Deserialize<TicketAiSummaryDto>(cached, JsonOpts);
                if (hit is not null) return hit with { WasFromCache = true };
            }
        }

        var ticket = await db.Tickets
            .AsNoTracking()
            .Where(t => t.Id == request.TicketId)
            .Select(t => new
            {
                t.Id, t.Title, t.Description,
                IssueType = t.IssueType!.Name,
                Status = t.Status!.Name,
                Priority = t.Priority != null ? t.Priority.Name : "None",
                Assignee = t.Assignee != null ? t.Assignee.DisplayName : "Unassigned",
                t.CreatedAt, t.UpdatedAt, t.DueDate
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        var comments = await db.Comments
            .AsNoTracking()
            .Where(c => c.TicketId == request.TicketId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Take(30)
            .Select(c => new { Author = c.Author.DisplayName, c.Body, c.CreatedAt })
            .ToListAsync(ct);

        var commentBlock = comments.Count > 0
            ? string.Join("\n", comments.Select(c => $"[{c.Author} – {c.CreatedAt:g}]: {c.Body}"))
            : "No comments.";

        var rawContent = $"""
            TICKET: {ticket.Title}
            TYPE: {ticket.IssueType} | STATUS: {ticket.Status} | PRIORITY: {ticket.Priority}
            ASSIGNEE: {ticket.Assignee} | DUE: {ticket.DueDate?.ToString("yyyy-MM-dd") ?? "N/A"}

            DESCRIPTION:
            {ticket.Description ?? "(none)"}

            COMMENT THREAD:
            {commentBlock}
            """;

        var userPrompt = masker.Mask(rawContent);

        const string systemPrompt = """
            You are an expert IT project analyst. Analyze the ticket and return ONLY valid JSON with this exact structure (no markdown, no code fences):
            {
              "executiveSummary": "2-3 sentence summary for executives",
              "technicalSummary": "technical details for engineers",
              "simpleExplanation": "plain-language explanation for non-technical readers",
              "keyDecisions": ["decision 1"],
              "blockers": ["blocker 1"],
              "risks": ["risk 1"],
              "actionItems": ["action 1"],
              "completionConfidencePercent": 70
            }
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, userPrompt, MaxTokens: 1200, Temperature: 0.2f,
                CorrelationId: request.TicketId.ToString()), ct);
        var latency = DateTimeOffset.UtcNow - start;

        SummaryJson parsed;
        try { parsed = JsonSerializer.Deserialize<SummaryJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { parsed = new SummaryJson { ExecutiveSummary = aiResp.Content, TechnicalSummary = aiResp.Content }; }

        var dto = new TicketAiSummaryDto(
            request.TicketId,
            parsed.ExecutiveSummary,
            parsed.TechnicalSummary,
            parsed.SimpleExplanation,
            parsed.KeyDecisions ?? [],
            parsed.Blockers ?? [],
            parsed.Risks ?? [],
            parsed.ActionItems ?? [],
            Math.Clamp(parsed.CompletionConfidencePercent, 0, 100),
            WasFromCache: false,
            DateTimeOffset.UtcNow);

        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(dto), CacheTtl, ct);
        await audit.LogAsync(new AiAuditEntry("summarize-ticket", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false,
            request.TicketId.ToString(), start), ct);

        return dto;
    }

    private sealed class SummaryJson
    {
        public string ExecutiveSummary { get; set; } = "";
        public string TechnicalSummary { get; set; } = "";
        public string SimpleExplanation { get; set; } = "";
        public List<string>? KeyDecisions { get; set; }
        public List<string>? Blockers { get; set; }
        public List<string>? Risks { get; set; }
        public List<string>? ActionItems { get; set; }
        public int CompletionConfidencePercent { get; set; } = 50;
    }
}
