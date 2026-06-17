using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using Microsoft.Extensions.Logging;

namespace ITS.Infrastructure.Ai;

public sealed class AiAuditService(ILogger<AiAuditService> logger) : IAiAuditService
{
    public Task LogAsync(AiAuditEntry entry, CancellationToken ct = default)
    {
        logger.LogInformation(
            "AI_AUDIT | Feature={Feature} | User={UserId} | Provider={Provider} | " +
            "PromptTokens={PromptTokens} | CompletionTokens={CompletionTokens} | " +
            "LatencyMs={LatencyMs} | Cache={WasFromCache} | Correlation={CorrelationId} | At={RequestedAt}",
            entry.Feature, entry.UserId, entry.ProviderName,
            entry.PromptTokens, entry.CompletionTokens,
            (int)entry.Latency.TotalMilliseconds, entry.WasFromCache,
            entry.CorrelationId ?? "N/A", entry.RequestedAt);

        return Task.CompletedTask;
    }
}
