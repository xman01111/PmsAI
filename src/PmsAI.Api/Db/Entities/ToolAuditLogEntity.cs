using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("ToolAuditLogs")]
public class ToolAuditLogEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid HotelId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ArgumentsJsonMasked { get; set; } = string.Empty;
    public string ResultCode { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
