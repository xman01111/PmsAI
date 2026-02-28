namespace PmsAI.Contracts.Chat;

public class CitationDto
{
    public string CitationId { get; set; } = string.Empty;
    public Guid DocId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public int? Page { get; set; }
    public string ChunkId { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Snippet { get; set; } = string.Empty;
}
