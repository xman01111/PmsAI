namespace PmsAI.Contracts.Chat;

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public List<CitationDto> Citations { get; set; } = new();
    public List<ToolCallResultDto> ToolCalls { get; set; } = new();
    public PendingActionDto? PendingAction { get; set; }
}
