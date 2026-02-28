using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PmsAI.Api.Options;

namespace PmsAI.Api.Ai;

public class DeepSeekChatClient : ILLMChatClient
{
    private readonly HttpClient _http;
    private readonly DeepSeekOptions _options;
    private readonly ILogger<DeepSeekChatClient> _logger;

    public DeepSeekChatClient(IHttpClientFactory httpClientFactory, IOptions<DeepSeekOptions> options, ILogger<DeepSeekChatClient> logger)
    {
        _http = httpClientFactory.CreateClient("DeepSeek");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChatCompletionResponse> ChatAsync(
        List<ChatMessage> messages,
        List<object>? tools = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = _options.ChatModel,
            ["messages"] = messages.Select(m => BuildMessageObj(m)).ToList(),
            ["max_tokens"] = _options.MaxTokens,
            ["temperature"] = _options.Temperature
        };

        if (tools is { Count: > 0 })
        {
            requestBody["tools"] = tools;
            requestBody["tool_choice"] = "auto";
        }

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        _logger.LogDebug("Sending request to DeepSeek: {Model}", _options.ChatModel);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var result = new ChatCompletionResponse();
        var choice = root.GetProperty("choices")[0];
        var finishReason = choice.GetProperty("finish_reason").GetString();
        result.FinishReason = finishReason;

        var msgEl = choice.GetProperty("message");
        var role = msgEl.GetProperty("role").GetString() ?? "assistant";
        var content = msgEl.TryGetProperty("content", out var contentEl) ? contentEl.GetString() : null;

        var assistantMsg = new ChatMessage { Role = role, Content = content };

        if (msgEl.TryGetProperty("tool_calls", out var toolCallsEl) && toolCallsEl.ValueKind == JsonValueKind.Array)
        {
            assistantMsg.ToolCalls = new List<ToolCall>();
            foreach (var tc in toolCallsEl.EnumerateArray())
            {
                assistantMsg.ToolCalls.Add(new ToolCall
                {
                    Id = tc.GetProperty("id").GetString() ?? string.Empty,
                    Type = tc.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "function" : "function",
                    Function = new FunctionCall
                    {
                        Name = tc.GetProperty("function").GetProperty("name").GetString() ?? string.Empty,
                        Arguments = tc.GetProperty("function").GetProperty("arguments").GetString() ?? string.Empty
                    }
                });
            }
        }

        result.Messages.Add(assistantMsg);
        return result;
    }

    private static object BuildMessageObj(ChatMessage m)
    {
        if (m.ToolCalls != null)
        {
            return new
            {
                role = m.Role,
                content = m.Content,
                tool_calls = m.ToolCalls.Select(tc => new
                {
                    id = tc.Id,
                    type = tc.Type,
                    function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
                }).ToList()
            };
        }

        if (m.ToolCallId != null)
        {
            return new { role = m.Role, content = m.Content, tool_call_id = m.ToolCallId, name = m.Name };
        }

        return new { role = m.Role, content = m.Content };
    }
}
