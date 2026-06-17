using System.Text;
using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITS.Infrastructure.Ai.Providers;

public sealed class AzureOpenAiProviderOptions
{
    public const string SectionName = "Ai:AzureOpenAi";
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string DeploymentName { get; set; } = "gpt-4o";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";
    public string ApiVersion { get; set; } = "2024-02-01";
}

public sealed class AzureOpenAiProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<AzureOpenAiProviderOptions> options,
    ILogger<AzureOpenAiProvider> logger)
    : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly AzureOpenAiProviderOptions _opts = options.Value;

    public string ProviderName => "azure-openai";
    public bool SupportsEmbeddings => true;

    public async Task<AiTextResponse> CompleteAsync(AiTextRequest request, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("azure-openai");
        var url = $"{_opts.Endpoint}/openai/deployments/{_opts.DeploymentName}/chat/completions?api-version={_opts.ApiVersion}";

        var body = new
        {
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        var httpContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var start = DateTime.UtcNow;

        try
        {
            using var resp = await client.PostAsync(url, httpContent, ct);
            resp.EnsureSuccessStatusCode();

            var raw = await resp.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<AzureChatResponse>(raw, JsonOpts)
                ?? throw new InvalidOperationException("Empty Azure OpenAI response");

            return new AiTextResponse(
                parsed.Choices[0].Message.Content,
                parsed.Usage?.PromptTokens ?? 0,
                parsed.Usage?.CompletionTokens ?? 0,
                ProviderName,
                DateTime.UtcNow - start);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Azure OpenAI call failed for {CorrelationId}", request.CorrelationId);
            throw;
        }
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("azure-openai");
        var url = $"{_opts.Endpoint}/openai/deployments/{_opts.EmbeddingDeploymentName}/embeddings?api-version={_opts.ApiVersion}";
        var body = new { input = text };
        var httpContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await client.PostAsync(url, httpContent, ct);
        resp.EnsureSuccessStatusCode();

        var raw = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<AzureEmbeddingResponse>(raw, JsonOpts)!;
        return parsed.Data[0].Embedding;
    }

    private sealed class AzureChatResponse
    {
        public List<Choice> Choices { get; set; } = [];
        public Usage? Usage { get; set; }
    }
    private sealed class Choice { public Message Message { get; set; } = new(); }
    private sealed class Message { public string Content { get; set; } = ""; }
    private sealed class Usage { public int PromptTokens { get; set; } public int CompletionTokens { get; set; } }
    private sealed class AzureEmbeddingResponse { public List<EmbeddingData> Data { get; set; } = []; }
    private sealed class EmbeddingData { public float[] Embedding { get; set; } = []; }
}
