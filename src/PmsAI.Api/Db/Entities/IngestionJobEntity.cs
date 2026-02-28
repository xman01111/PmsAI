using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("IngestionJobs")]
public class IngestionJobEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    [SugarColumn(IsNullable = true)]
    public Guid? HotelId { get; set; }
    public Guid DocId { get; set; }
    public string Status { get; set; } = string.Empty; // Queued/Running/Done/Failed
    [SugarColumn(IsNullable = true)]
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    [SugarColumn(IsNullable = true)]
    public DateTime? FinishedAt { get; set; }
}
