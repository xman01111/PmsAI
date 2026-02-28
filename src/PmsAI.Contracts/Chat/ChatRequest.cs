namespace PmsAI.Contracts.Chat;

public class ChatRequest
{
    public string? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? HotelRef { get; set; }
    public bool Stream { get; set; } = false;
}
