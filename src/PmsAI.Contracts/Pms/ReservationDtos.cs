namespace PmsAI.Contracts.Pms;

public class ReservationQueryDto
{
    public string? HotelRef { get; set; }
    public string? ReservationNo { get; set; }
    public string? GuestName { get; set; }
    public string? Phone { get; set; }
    public string? ArrivalDate { get; set; }
}

public class ReservationDto
{
    public string ReservationNo { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string RoomNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ArrivalDate { get; set; }
    public DateTimeOffset DepartureDate { get; set; }
}
