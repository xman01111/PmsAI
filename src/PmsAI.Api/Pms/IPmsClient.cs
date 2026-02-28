using PmsAI.Contracts.Pms;

namespace PmsAI.Api.Pms;

public interface IPmsClient
{
    Task<List<ReservationDto>> GetReservationsAsync(Guid hotelId, ReservationQueryDto query, CancellationToken cancellationToken = default);
    Task<List<RoomStatusDto>> GetRoomStatusAsync(Guid hotelId, RoomStatusQueryDto query, CancellationToken cancellationToken = default);
    Task<WorkOrderResultDto> CreateWorkOrderAsync(Guid hotelId, CreateWorkOrderDto dto, CancellationToken cancellationToken = default);
    Task<LateCheckoutResultDto> RequestLateCheckoutAsync(Guid hotelId, LateCheckoutRequestDto dto, CancellationToken cancellationToken = default);
}
