namespace PmsAI.Contracts.Pms;

public class RoomStatusQueryDto
{
    public string? HotelRef { get; set; }
    public string? Date { get; set; }
}

public class RoomStatusDto
{
    public string RoomNo { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Vacant/Occupied/Reserved/Maintenance
    public string? GuestName { get; set; }
}
