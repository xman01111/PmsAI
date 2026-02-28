namespace PmsAI.Contracts.Ingest;

public class IngestResponse
{
    public Guid JobId { get; set; }
    public Guid DocId { get; set; }
    public string Status { get; set; } = "Queued";
}
