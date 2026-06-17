namespace ITS.Application.Common.Models.Ai;

public sealed record AiTextRequest(
    string SystemPrompt,
    string UserPrompt,
    int MaxTokens = 1500,
    float Temperature = 0.3f,
    string? CorrelationId = null
);
