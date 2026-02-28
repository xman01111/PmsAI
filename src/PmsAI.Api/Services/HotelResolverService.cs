using PmsAI.Api.Db.Repositories;
using PmsAI.Api.Models;

namespace PmsAI.Api.Services;

public class HotelResolverService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ILogger<HotelResolverService> _logger;

    public HotelResolverService(IHotelRepository hotelRepository, ILogger<HotelResolverService> logger)
    {
        _hotelRepository = hotelRepository;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a hotelRef string (name/alias/GUID) to a HotelId GUID.
    /// Always validates that the resolved hotel is within the user's authorized hotels.
    /// </summary>
    public async Task<Guid?> ResolveAsync(TenantContext tenantContext, string? hotelRef, CancellationToken cancellationToken = default)
    {
        if (tenantContext.HotelIds.Count == 1 && string.IsNullOrEmpty(hotelRef))
        {
            return tenantContext.HotelIds[0];
        }

        if (string.IsNullOrEmpty(hotelRef))
        {
            _logger.LogDebug("No hotelRef provided and user has multiple hotels");
            return null;
        }

        // Try parse as GUID
        if (Guid.TryParse(hotelRef, out var parsedGuid))
        {
            if (!tenantContext.HotelIds.Contains(parsedGuid))
            {
                _logger.LogWarning("User attempted to access unauthorized hotel: {HotelId}", parsedGuid);
                return null;
            }
            return parsedGuid;
        }

        // Look up by alias/name
        var hotel = await _hotelRepository.FindByAliasAsync(tenantContext.TenantId, hotelRef);
        if (hotel == null)
        {
            _logger.LogDebug("Hotel not found by ref: {HotelRef}", hotelRef);
            return null;
        }

        if (!tenantContext.HotelIds.Contains(hotel.HotelId))
        {
            _logger.LogWarning("User attempted to access unauthorized hotel: {HotelId}", hotel.HotelId);
            return null;
        }

        return hotel.HotelId;
    }
}
