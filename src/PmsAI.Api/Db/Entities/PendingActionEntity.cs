using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("PendingActions")]
public class PendingActionEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid PendingActionId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid HotelId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty; // 给用户看的中文摘要
    public string Status { get; set; } = string.Empty; // Pending/Confirmed/Cancelled/Expired
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
