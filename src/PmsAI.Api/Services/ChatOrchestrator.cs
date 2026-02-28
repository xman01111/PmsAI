using System.Text;
using System.Text.Json;
using PmsAI.Api.Ai;
using PmsAI.Api.Models;
using PmsAI.Api.Pms.Tools;
using PmsAI.Api.Rag;
using PmsAI.Api.Rag.VectorStore;
using PmsAI.Api.Services;
using PmsAI.Contracts.Chat;

namespace PmsAI.Api.Services;

public class ChatOrchestrator
{
    private readonly ILLMChatClient _llm;
    private readonly IEmbeddingClient _embedding;
    private readonly IVectorStore _vectorStore;
    private readonly ContextBuilder _contextBuilder;
    private readonly PmsToolExecutor _toolExecutor;
    private readonly AuditService _auditService;
    private readonly ILogger<ChatOrchestrator> _logger;

    private const string SystemPromptPath = "Prompts/SystemPrompt.txt";

    public ChatOrchestrator(
        ILLMChatClient llm,
        IEmbeddingClient embedding,
        IVectorStore vectorStore,
        ContextBuilder contextBuilder,
        PmsToolExecutor toolExecutor,
        AuditService auditService,
        ILogger<ChatOrchestrator> logger)
    {
        _llm = llm;
        _embedding = embedding;
        _vectorStore = vectorStore;
        _contextBuilder = contextBuilder;
        _toolExecutor = toolExecutor;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(
        ChatRequest request,
        TenantContext tenantContext,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var conversationId = string.IsNullOrEmpty(request.ConversationId)
            ? Guid.NewGuid().ToString()
            : request.ConversationId;

        // 1. Embed user message
        float[] queryVector;
        try
        {
            queryVector = await _embedding.EmbedAsync(request.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding failed, proceeding without RAG");
            queryVector = Array.Empty<float>();
        }

        // 2. RAG retrieval
        List<PmsAI.Api.Rag.VectorStore.ChunkSearchResult> chunks = new();
        if (queryVector.Length > 0)
        {
            var hotelId = tenantContext.HotelIds.Count == 1 ? tenantContext.HotelIds[0].ToString() : null;
            try
            {
                chunks = await _vectorStore.SearchAsync(
                    queryVector,
                    tenantContext.TenantId.ToString(),
                    hotelId,
                    tenantContext.Roles,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector search failed");
            }
        }

        var (ragContext, citations) = _contextBuilder.Build(chunks);

        // 3. Build messages
        var systemPrompt = await LoadSystemPromptAsync();
        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = BuildSystemContent(systemPrompt, ragContext) },
            new() { Role = "user", Content = request.Message }
        };

        // 4. First LLM call with tools
        var toolDefs = PmsToolDefinitions.GetAllTools();
        var llmResponse = await _llm.ChatAsync(messages, toolDefs, cancellationToken);

        var toolCallResults = new List<ToolCallResultDto>();
        PmsAI.Api.Db.Entities.PendingActionEntity? pendingAction = null;
        string finalAnswer = string.Empty;

        var assistantMsg = llmResponse.Messages.FirstOrDefault();

        // 5. Handle tool calls
        if (assistantMsg?.ToolCalls != null && assistantMsg.ToolCalls.Count > 0)
        {
            messages.Add(assistantMsg);

            foreach (var toolCall in assistantMsg.ToolCalls)
            {
                _logger.LogInformation("Tool call: {Tool}", toolCall.Function.Name);

                var result = await _toolExecutor.ExecuteAsync(
                    toolCall.Function.Name,
                    toolCall.Function.Arguments,
                    tenantContext,
                    conversationId,
                    cancellationToken);

                if (result.IsPending)
                {
                    pendingAction = result.PendingAction;
                    toolCallResults.Add(new ToolCallResultDto
                    {
                        ToolName = toolCall.Function.Name,
                        Executed = false,
                        ResultSummary = result.PendingAction?.Summary
                    });

                    // Add tool result message with pending indication
                    messages.Add(new ChatMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Name = toolCall.Function.Name,
                        Content = JsonSerializer.Serialize(new { status = "pending_confirmation", summary = result.PendingAction?.Summary })
                    });
                }
                else if (result.IsSuccess)
                {
                    toolCallResults.Add(new ToolCallResultDto
                    {
                        ToolName = toolCall.Function.Name,
                        Executed = true,
                        ResultSummary = SummarizeResult(result.ResultJson)
                    });

                    messages.Add(new ChatMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Name = toolCall.Function.Name,
                        Content = result.ResultJson
                    });
                }
                else
                {
                    toolCallResults.Add(new ToolCallResultDto
                    {
                        ToolName = toolCall.Function.Name,
                        Executed = false,
                        ResultSummary = result.ErrorMessage
                    });

                    messages.Add(new ChatMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Name = toolCall.Function.Name,
                        Content = JsonSerializer.Serialize(new { error = result.ErrorMessage })
                    });
                }
            }

            // 6. Second LLM call for final answer
            var finalResponse = await _llm.ChatAsync(messages, null, cancellationToken);
            finalAnswer = finalResponse.Messages.FirstOrDefault()?.Content ?? string.Empty;
        }
        else
        {
            finalAnswer = assistantMsg?.Content ?? string.Empty;
        }

        sw.Stop();
        await _auditService.LogChatAsync(
            tenantContext,
            conversationId,
            request.Message.Length,
            citations.Count,
            citations.Count > 0,
            (int)sw.ElapsedMilliseconds);

        return new ChatResponse
        {
            Answer = finalAnswer,
            ConversationId = conversationId,
            Citations = citations,
            ToolCalls = toolCallResults,
            PendingAction = pendingAction != null ? MapPendingAction(pendingAction) : null
        };
    }

    private static string BuildSystemContent(string systemPrompt, string ragContext)
    {
        if (string.IsNullOrEmpty(ragContext)) return systemPrompt;

        return $"{systemPrompt}\n\n## 知识库上下文\n{ragContext}";
    }

    private static async Task<string> LoadSystemPromptAsync()
    {
        try
        {
            if (File.Exists(SystemPromptPath))
                return await File.ReadAllTextAsync(SystemPromptPath);
        }
        catch { }

        return "你是一个专业的酒店AI助手，基于知识库回答问题并协助处理酒店业务。回答时请准确、专业，并引用相关知识库内容。";
    }

    private static string SummarizeResult(string? json)
    {
        if (string.IsNullOrEmpty(json)) return string.Empty;
        return json.Length > 200 ? json[..200] + "..." : json;
    }

    private static PendingActionDto MapPendingAction(PmsAI.Api.Db.Entities.PendingActionEntity entity)
    {
        object? args = null;
        try { args = JsonSerializer.Deserialize<object>(entity.ArgumentsJson); } catch { }

        return new PendingActionDto
        {
            PendingActionId = entity.PendingActionId,
            ToolName = entity.ToolName,
            Summary = entity.Summary,
            Arguments = args,
            ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(entity.ExpiresAt, DateTimeKind.Utc))
        };
    }
}
