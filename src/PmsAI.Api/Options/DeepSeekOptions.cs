namespace PmsAI.Api.Options;

public class DeepSeekOptions
{
    public string BaseUrl { get; set; } = "https://api.deepseek.com";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "deepseek-chat";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
}
