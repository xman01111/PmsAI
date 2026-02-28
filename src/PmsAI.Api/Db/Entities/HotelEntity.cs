using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("Hotels")]
public class HotelEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid HotelId { get; set; }
    public Guid TenantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "Asia/Shanghai";
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
