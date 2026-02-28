using PmsAI.Api.Db.Entities;
using SqlSugar;

namespace PmsAI.Api.Db.Repositories;

public class TenantRepository : SqlSugarRepositoryBase<TenantEntity>, ITenantRepository
{
    public TenantRepository(ISqlSugarClient db) : base(db) { }

    public async Task<TenantEntity?> GetByIdAsync(Guid tenantId)
    {
        return await _db.Queryable<TenantEntity>()
            .Where(t => t.TenantId == tenantId)
            .FirstAsync();
    }
}
