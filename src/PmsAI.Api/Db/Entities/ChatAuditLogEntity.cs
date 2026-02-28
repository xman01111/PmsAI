using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("ChatAuditLogs")]
public class ChatAuditLogEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public int QuestionLength { get; set; }
    public int CitationCount { get; set; }
    public bool HitKnowledgeBase { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
