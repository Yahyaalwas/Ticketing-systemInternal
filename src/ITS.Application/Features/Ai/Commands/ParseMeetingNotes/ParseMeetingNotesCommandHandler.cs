using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;

namespace ITS.Application.Features.Ai.Commands.ParseMeetingNotes;

public sealed class ParseMeetingNotesCommandHandler(
    IAiProvider ai,
    IAiRateLimiter rateLimiter,
    IAiAuditService audit,
    IAiDataMasker masker,
    ICurrentUserService currentUser)
    : IRequestHandler<ParseMeetingNotesCommand, MeetingParseResultDto>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<MeetingParseResultDto> Handle(ParseMeetingNotesCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "parse-meeting", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        var userPrompt = masker.Mask($"""
            MEETING: {request.MeetingTitle ?? "Untitled"}
            DATE: {request.MeetingDate?.ToString("yyyy-MM-dd") ?? "Unknown"}

            NOTES:
            {request.RawNotes}
            """);

        const string systemPrompt = """
            You are an expert meeting analyst. Extract structured information from meeting notes and return ONLY valid JSON (no markdown):
            {
              "meetingTitle": "Title",
              "narrativeSummary": "2-3 sentence summary",
              "tasks": [
                { "title": "Task title", "description": "details", "owner": "Name or null", "dueDate": "YYYY-MM-DD or null", "priority": "High|Medium|Low" }
              ],
              "decisions": [
                { "decision": "What was decided", "context": "Why", "owner": "Who is responsible or null" }
              ],
              "risks": ["risk 1", "risk 2"],
              "dependencies": ["dependency 1"]
            }
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, userPrompt, MaxTokens: 2000, Temperature: 0.2f), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("parse-meeting", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false, null, start), ct);

        MeetingJson parsed;
        try { parsed = JsonSerializer.Deserialize<MeetingJson>(aiResp.Content, JsonOpts) ?? new(); }
        catch { parsed = new MeetingJson { NarrativeSummary = aiResp.Content }; }

        return new MeetingParseResultDto(
            parsed.MeetingTitle ?? request.MeetingTitle ?? "Meeting",
            (parsed.Tasks ?? []).Select(t => new ExtractedTask(t.Title, t.Description, t.Owner, t.DueDate, t.Priority ?? "Medium")).ToList(),
            (parsed.Decisions ?? []).Select(d => new ExtractedDecision(d.Decision, d.Context, d.Owner)).ToList(),
            parsed.Risks ?? [],
            parsed.Dependencies ?? [],
            parsed.NarrativeSummary ?? "",
            DateTimeOffset.UtcNow);
    }

    private sealed class MeetingJson
    {
        public string? MeetingTitle { get; set; }
        public string? NarrativeSummary { get; set; }
        public List<TaskJson>? Tasks { get; set; }
        public List<DecisionJson>? Decisions { get; set; }
        public List<string>? Risks { get; set; }
        public List<string>? Dependencies { get; set; }
    }
    private sealed class TaskJson { public string Title { get; set; } = ""; public string? Description { get; set; } public string? Owner { get; set; } public string? DueDate { get; set; } public string? Priority { get; set; } }
    private sealed class DecisionJson { public string Decision { get; set; } = ""; public string? Context { get; set; } public string? Owner { get; set; } }
}
