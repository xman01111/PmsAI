using PmsAI.Api.Db.Entities;

namespace PmsAI.Api.Db.Repositories;

public interface IPendingActionRepository
{
    Task<PendingActionEntity?> GetByIdAsync(Guid pendingActionId);
    Task<int> InsertAsync(PendingActionEntity entity);
    Task<int> UpdateAsync(PendingActionEntity entity);
    Task ExpireOldActionsAsync();
}
