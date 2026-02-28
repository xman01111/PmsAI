using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("HotelAliases")]
public class HotelAliasEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid TenantId { get; set; }
    public string Alias { get; set; } = string.Empty;
}
