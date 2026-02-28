using PmsAI.Api.Db.Entities;
using SqlSugar;

namespace PmsAI.Api.Db.Repositories;

public class HotelRepository : SqlSugarRepositoryBase<HotelEntity>, IHotelRepository
{
    public HotelRepository(ISqlSugarClient db) : base(db) { }

    public async Task<HotelEntity?> GetByIdAsync(Guid hotelId)
    {
        return await _db.Queryable<HotelEntity>()
            .Where(h => h.HotelId == hotelId)
            .FirstAsync();
    }

    public async Task<List<HotelEntity>> GetByTenantAsync(Guid tenantId)
    {
        return await _db.Queryable<HotelEntity>()
            .Where(h => h.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<HotelEntity?> FindByAliasAsync(Guid tenantId, string alias)
    {
        var lowerAlias = alias.ToLower();
        // Search by DisplayName, ShortName
        var hotel = await _db.Queryable<HotelEntity>()
            .Where(h => h.TenantId == tenantId &&
                (h.DisplayName.ToLower() == lowerAlias || h.ShortName.ToLower() == lowerAlias))
            .FirstAsync();

        if (hotel != null) return hotel;

        // Search in HotelAliases
        var aliasEntity = await _db.Queryable<HotelAliasEntity>()
            .Where(a => a.TenantId == tenantId && a.Alias.ToLower() == lowerAlias)
            .FirstAsync();

        if (aliasEntity == null) return null;

        return await _db.Queryable<HotelEntity>()
            .Where(h => h.HotelId == aliasEntity.HotelId)
            .FirstAsync();
    }
}
