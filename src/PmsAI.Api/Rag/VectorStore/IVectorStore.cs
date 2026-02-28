namespace PmsAI.Api.Rag.VectorStore;

public class ChunkSearchResult
{
    public string ChunkId { get; set; } = string.Empty;
    public string DocId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public int? Page { get; set; }
    public string? Section { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class ChunkPayload
{
    public string TenantId { get; set; } = string.Empty;
    public string? HotelId { get; set; }
    public List<string>? Roles { get; set; }
    public string DocId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public int? Page { get; set; }
    public string? Section { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(string pointId, float[] vector, ChunkPayload payload, CancellationToken cancellationToken = default);
    Task<List<ChunkSearchResult>> SearchAsync(float[] queryVector, string tenantId, string? hotelId, List<string>? roles, CancellationToken cancellationToken = default);
    Task DeleteByDocIdAsync(string tenantId, string docId, CancellationToken cancellationToken = default);
}
