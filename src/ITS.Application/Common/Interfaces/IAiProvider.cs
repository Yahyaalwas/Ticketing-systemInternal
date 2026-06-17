using ITS.Application.Common.Models.Ai;

namespace ITS.Application.Common.Interfaces;

public interface IAiProvider
{
    string ProviderName { get; }
    bool SupportsEmbeddings { get; }
    Task<AiTextResponse> CompleteAsync(AiTextRequest request, CancellationToken ct = default);
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
