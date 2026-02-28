namespace PmsAI.Api.Ai;

public interface IEmbeddingClient
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
    Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken cancellationToken = default);
}
