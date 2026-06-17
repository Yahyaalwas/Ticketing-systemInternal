using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;

namespace ITS.Application.Features.Ai.Commands.DraftTicketFromText;

public sealed class DraftTicketFromTextCommandHandler(
    IAiProvider ai,
    IAiRateLimiter rateLimiter,
    IAiAuditService audit,
    IAiDataMasker masker,
    ICurrentUserService currentUser)
    : IRequestHandler<DraftTicketFromTextCommand, TicketDraftDto>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<TicketDraftDto> Handle(DraftTicketFromTextCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "draft-ticket", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        const string systemPrompt = """
            You are an IT ticketing system expert. Given raw text (email, meeting notes, or description), extract a structured ticket draft and return ONLY valid JSON:
            {
              "suggestedTitle": "Short, clear title (max 100 chars)",
              "suggestedDescription": "Detailed markdown description",
              "suggestedPriority": "Critical|High|Medium|Low",
              "suggestedIssueType": "Bug|Feature|Task|Improvement|Question|Incident",
              "suggestedLabels": ["label1", "label2"],
              "suggestedAssigneeName": "Name or null",
              "suggestedDueDate": "YYYY-MM-DD or null"
            }
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, masker.Mask(request.RawText), MaxTokens: 1000, Temperature: 0.3f), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("draft-ticket", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false, null, start), ct);

        DraftJson parsed;
        try { parsed = JsonSerializer.Deserialize<DraftJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { parsed = new DraftJson { SuggestedTitle = "New Ticket", SuggestedDescription = request.RawText }; }

        return new TicketDraftDto(
            parsed.SuggestedTitle ?? "New Ticket",
            parsed.SuggestedDescription ?? request.RawText,
            parsed.SuggestedPriority ?? "Medium",
            parsed.SuggestedIssueType ?? "Task",
            parsed.SuggestedLabels ?? [],
            parsed.SuggestedAssigneeName,
            parsed.SuggestedDueDate,
            aiResp.ProviderName,
            DateTimeOffset.UtcNow);
    }

    private sealed class DraftJson
    {
        public string? SuggestedTitle { get; set; }
        public string? SuggestedDescription { get; set; }
        public string? SuggestedPriority { get; set; }
        public string? SuggestedIssueType { get; set; }
        public List<string>? SuggestedLabels { get; set; }
        public string? SuggestedAssigneeName { get; set; }
        public string? SuggestedDueDate { get; set; }
    }
}
