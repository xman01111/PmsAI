using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("UserHotels")]
public class UserHotelEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string RolesCsv { get; set; } = string.Empty; // 逗号分隔的角色代码
    public DateTime CreatedAt { get; set; }
}
