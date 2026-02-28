namespace PmsAI.Api.Models;

public class TenantContext
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public List<Guid> HotelIds { get; set; } = new();
    public List<string> Roles { get; set; } = new();
}
