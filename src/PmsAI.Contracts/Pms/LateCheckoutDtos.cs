namespace PmsAI.Contracts.Pms;

public class LateCheckoutRequestDto
{
    public string? HotelRef { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string RequestedCheckoutTime { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class LateCheckoutResultDto
{
    public bool Approved { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset? ApprovedCheckoutTime { get; set; }
}
