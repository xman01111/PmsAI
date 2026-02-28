using PmsAI.Api.Db.Entities;

namespace PmsAI.Api.Db.Repositories;

public interface IAuditRepository
{
    Task InsertToolAuditAsync(ToolAuditLogEntity entity);
    Task InsertChatAuditAsync(ChatAuditLogEntity entity);
}
