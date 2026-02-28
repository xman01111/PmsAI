namespace PmsAI.Api.Options;

public class QwenEmbeddingOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-v3";
    public int BatchSize { get; set; } = 25;
}
