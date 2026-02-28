namespace PmsAI.Contracts.Chat;

public class ToolCallResultDto
{
    public string ToolName { get; set; } = string.Empty;
    public bool Executed { get; set; }
    public string? ResultSummary { get; set; }
}
