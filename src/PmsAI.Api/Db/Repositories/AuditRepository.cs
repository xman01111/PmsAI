using PmsAI.Api.Db.Entities;
using SqlSugar;

namespace PmsAI.Api.Db.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly ISqlSugarClient _db;

    public AuditRepository(ISqlSugarClient db) => _db = db;

    public async Task InsertToolAuditAsync(ToolAuditLogEntity entity)
    {
        await _db.Insertable(entity).ExecuteCommandAsync();
    }

    public async Task InsertChatAuditAsync(ChatAuditLogEntity entity)
    {
        await _db.Insertable(entity).ExecuteCommandAsync();
    }
}
