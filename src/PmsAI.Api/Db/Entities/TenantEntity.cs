using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("Tenants")]
public class TenantEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Active/Inactive
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
