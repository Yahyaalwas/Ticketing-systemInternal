using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITS.Infrastructure.Ai.Providers;

public sealed class OpenAiProviderOptions
{
    public const string SectionName = "Ai:OpenAi";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public sealed class OpenAiProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiProviderOptions> options,
    ILogger<OpenAiProvider> logger)
    : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly OpenAiProviderOptions _opts = options.Value;

    public string ProviderName => "openai";
    public bool SupportsEmbeddings => true;

    public async Task<AiTextResponse> CompleteAsync(AiTextRequest request, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("openai");
        var body = new
        {
            model = _opts.Model,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var start = DateTime.UtcNow;
        try
        {
            using var resp = await client.PostAsync("/v1/chat/completions", httpContent, ct);
            resp.EnsureSuccessStatusCode();

            var raw = await resp.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(raw, JsonOpts)
                ?? throw new InvalidOperationException("Empty OpenAI response");

            var latency = DateTime.UtcNow - start;
            return new AiTextResponse(
                parsed.Choices[0].Message.Content,
                parsed.Usage?.PromptTokens ?? 0,
                parsed.Usage?.CompletionTokens ?? 0,
                ProviderName,
                latency);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "OpenAI API call failed for request {CorrelationId}", request.CorrelationId);
            throw;
        }
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("openai");
        var body = new { model = _opts.EmbeddingModel, input = text };
        var httpContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await client.PostAsync("/v1/embeddings", httpContent, ct);
        resp.EnsureSuccessStatusCode();

        var raw = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(raw, JsonOpts)!;
        return parsed.Data[0].Embedding;
    }

    private sealed class OpenAiChatResponse
    {
        public List<Choice> Choices { get; set; } = [];
        public Usage? Usage { get; set; }
    }
    private sealed class Choice { public Message Message { get; set; } = new(); }
    private sealed class Message { public string Content { get; set; } = ""; }
    private sealed class Usage { public int PromptTokens { get; set; } public int CompletionTokens { get; set; } }
    private sealed class OpenAiEmbeddingResponse { public List<EmbeddingData> Data { get; set; } = []; }
    private sealed class EmbeddingData { public float[] Embedding { get; set; } = []; }
}
