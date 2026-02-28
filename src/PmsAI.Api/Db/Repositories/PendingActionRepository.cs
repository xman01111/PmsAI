using PmsAI.Api.Db.Entities;
using SqlSugar;

namespace PmsAI.Api.Db.Repositories;

public class PendingActionRepository : SqlSugarRepositoryBase<PendingActionEntity>, IPendingActionRepository
{
    public PendingActionRepository(ISqlSugarClient db) : base(db) { }

    public async Task<PendingActionEntity?> GetByIdAsync(Guid pendingActionId)
    {
        return await _db.Queryable<PendingActionEntity>()
            .Where(p => p.PendingActionId == pendingActionId)
            .FirstAsync();
    }

    public new Task<int> InsertAsync(PendingActionEntity entity)
        => _db.Insertable(entity).ExecuteCommandAsync();

    public new Task<int> UpdateAsync(PendingActionEntity entity)
        => _db.Updateable(entity).ExecuteCommandAsync();

    public async Task ExpireOldActionsAsync()
    {
        await _db.Updateable<PendingActionEntity>()
            .SetColumns(p => new PendingActionEntity { Status = "Expired" })
            .Where(p => p.Status == "Pending" && p.ExpiresAt < DateTime.UtcNow)
            .ExecuteCommandAsync();
    }
}
