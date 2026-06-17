namespace ITS.Infrastructure.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Active provider: "mock", "openai", "azure-openai"</summary>
    public string Provider { get; set; } = "mock";

    public bool Enabled { get; set; } = true;
}
