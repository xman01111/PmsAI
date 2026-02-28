namespace PmsAI.Api.Options;

public class JwtOptions
{
    public string Issuer { get; set; } = "PmsAI";
    public string Audience { get; set; } = "PmsAI";
    public string SecretKey { get; set; } = string.Empty;
}
