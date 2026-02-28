using PmsAI.Api.Db.Entities;

namespace PmsAI.Api.Db.Repositories;

public interface ITenantRepository
{
    Task<TenantEntity?> GetByIdAsync(Guid tenantId);
}
