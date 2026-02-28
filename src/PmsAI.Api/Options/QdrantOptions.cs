namespace PmsAI.Api.Options;

public class QdrantOptions
{
    public string Url { get; set; } = "http://localhost:6333";
    public string CollectionName { get; set; } = "kb_chunks";
    public int VectorSize { get; set; } = 1024;
    public int TopK { get; set; } = 8;
    public double ScoreThreshold { get; set; } = 0.75;
}
