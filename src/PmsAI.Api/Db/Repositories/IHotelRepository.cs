using PmsAI.Api.Db.Entities;

namespace PmsAI.Api.Db.Repositories;

public interface IHotelRepository
{
    Task<HotelEntity?> GetByIdAsync(Guid hotelId);
    Task<List<HotelEntity>> GetByTenantAsync(Guid tenantId);
    Task<HotelEntity?> FindByAliasAsync(Guid tenantId, string alias);
}
