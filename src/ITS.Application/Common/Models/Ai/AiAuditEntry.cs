namespace ITS.Application.Common.Models.Ai;

public sealed record AiAuditEntry(
    string Feature,
    string UserId,
    string ProviderName,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Latency,
    bool WasFromCache,
    string? CorrelationId,
    DateTimeOffset RequestedAt
);
