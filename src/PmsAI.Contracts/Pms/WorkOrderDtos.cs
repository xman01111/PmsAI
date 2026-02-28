namespace PmsAI.Contracts.Pms;

public class CreateWorkOrderDto
{
    public string? HotelRef { get; set; }
    public string RoomNo { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class WorkOrderResultDto
{
    public string WorkOrderNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
