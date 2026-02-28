namespace PmsAI.Contracts.Chat;

public class PendingActionDto
{
    public Guid PendingActionId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public object? Arguments { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
