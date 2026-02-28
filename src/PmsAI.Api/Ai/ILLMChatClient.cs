namespace PmsAI.Api.Ai;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string? Content { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
    public string? Name { get; set; }
}

public class ToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "function";
    public FunctionCall Function { get; set; } = new();
}

public class FunctionCall
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

public class ChatCompletionResponse
{
    public List<ChatMessage> Messages { get; set; } = new();
    public string? FinishReason { get; set; }
}

public interface ILLMChatClient
{
    Task<ChatCompletionResponse> ChatAsync(
        List<ChatMessage> messages,
        List<object>? tools = null,
        CancellationToken cancellationToken = default);
}
