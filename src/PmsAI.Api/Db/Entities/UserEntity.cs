using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("Users")]
public class UserEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
