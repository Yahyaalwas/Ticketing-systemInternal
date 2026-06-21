using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Queries.AnswerKnowledgeQuestion;

public sealed class AnswerKnowledgeQuestionQueryHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiRateLimiter rateLimiter,
    IAiAuditService audit,
    IAiDataMasker masker,
    ICurrentUserService currentUser)
    : IRequestHandler<AnswerKnowledgeQuestionQuery, KnowledgeAnswerDto>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<KnowledgeAnswerDto> Handle(AnswerKnowledgeQuestionQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "knowledge-qa", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        // Text-based search: find relevant tickets via keyword matching
        var keywords = request.Question.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(5)
            .ToArray();

        var ticketQuery = db.Tickets.AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (request.ProjectId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.ProjectId == request.ProjectId.Value);

        // Simple keyword search across title and description
        foreach (var kw in keywords)
        {
            var k = kw;
            ticketQuery = ticketQuery.Where(t =>
                t.Title.Contains(k) ||
                (t.Description != null && t.Description.Contains(k)));
        }

        var ticketsRaw = await ticketQuery
            .OrderByDescending(t => t.UpdatedAt)
            .Take(15)
            .Select(t => new
            {
                t.Id, t.TicketNumber, t.ProjectId, t.Title,
                t.StatusId, t.ResolutionId, t.Description
            })
            .ToListAsync(ct);

        // Bulk-load reference data
        var ticketStatusIds = ticketsRaw.Select(t => t.StatusId).Distinct().ToList();
        var ticketResolutionIds = ticketsRaw.Where(t => t.ResolutionId.HasValue).Select(t => t.ResolutionId!.Value).Distinct().ToList();
        var ticketProjectIds = ticketsRaw.Select(t => t.ProjectId).Distinct().ToList();

        var ticketStatusMap = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => ticketStatusIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var ticketResolutionMap = await db.Resolutions.AsNoTracking()
            .Where(r => ticketResolutionIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name, ct);
        var ticketProjectMap = await db.Projects.AsNoTracking()
            .Where(p => ticketProjectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ProjectKey, ct);

        var tickets = ticketsRaw.Select(t => new
        {
            t.Id,
            TicketKey = $"{ticketProjectMap.GetValueOrDefault(t.ProjectId, "")}-{t.TicketNumber}",
            t.Title,
            Status = ticketStatusMap.GetValueOrDefault(t.StatusId, "Unknown"),
            t.Description,
            Resolution = t.ResolutionId.HasValue ? ticketResolutionMap.GetValueOrDefault(t.ResolutionId.Value) : null
        }).ToList();

        if (tickets.Count == 0)
        {
            return new KnowledgeAnswerDto(
                "I couldn't find any relevant tickets matching your question.",
                [],
                "none",
                DateTimeOffset.UtcNow);
        }

        var context = string.Join("\n\n", tickets.Select((t, i) =>
            $"[{i + 1}] {t.TicketKey}: {t.Title}\nStatus: {t.Status}\nDescription: {t.Description ?? "(none)"}\nResolution: {t.Resolution ?? "N/A"}"));

        var rawPrompt = $"""
            QUESTION: {request.Question}

            RELEVANT TICKETS:
            {context}
            """;

        const string systemPrompt = """
            You are a knowledge base assistant for an IT ticketing system. Answer the user's question using only the provided ticket data. Return ONLY valid JSON:
            {
              "answer": "Clear, direct answer based on the tickets",
              "citations": [
                { "index": 1, "relevance": "Why this ticket is relevant" }
              ]
            }
            If no tickets are relevant, say so clearly in the answer.
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, masker.Mask(rawPrompt), MaxTokens: 800, Temperature: 0.3f), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("knowledge-qa", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false, null, start), ct);

        AnswerJson parsed;
        try { parsed = JsonSerializer.Deserialize<AnswerJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { parsed = new AnswerJson { Answer = aiResp.Content }; }

        var citations = new List<CitedTicket>();
        foreach (var c in parsed.Citations ?? [])
        {
            var idx = c.Index - 1;
            if (idx < 0 || idx >= tickets.Count) continue;
            var t = tickets[idx];
            citations.Add(new CitedTicket(t.Id, t.TicketKey, t.Title, t.Status, c.Relevance ?? "Related"));
        }

        return new KnowledgeAnswerDto(parsed.Answer ?? "", citations, aiResp.ProviderName, DateTimeOffset.UtcNow);
    }

    private sealed class AnswerJson { public string? Answer { get; set; } public List<CitationEntry>? Citations { get; set; } }
    private sealed class CitationEntry { public int Index { get; set; } public string? Relevance { get; set; } }
}
