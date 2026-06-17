namespace ITS.Application.Common.Models.Ai;

public sealed record AiTextResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    string ProviderName,
    TimeSpan Latency
);
