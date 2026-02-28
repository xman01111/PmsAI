namespace PmsAI.Contracts.Ingest;

public class IngestRequest
{
    public Guid? HotelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Roles { get; set; }
}
