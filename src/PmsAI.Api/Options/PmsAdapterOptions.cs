namespace PmsAI.Api.Options;

public class PmsAdapterOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5100/pms/v1";
    public int TimeoutSeconds { get; set; } = 30;
}
