using PmsAI.Api.Db.Entities;
using PmsAI.Api.Db.Repositories;
using PmsAI.Api.Models;

namespace PmsAI.Api.Services;

public class AuditService
{
    private readonly IAuditRepository _repository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditRepository repository, ILogger<AuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task LogToolCallAsync(
        TenantContext tenantContext,
        string toolName,
        string argumentsJson,
        string resultCode,
        int durationMs)
    {
        try
        {
            var entity = new ToolAuditLogEntity
            {
                TenantId = tenantContext.TenantId,
                UserId = tenantContext.UserId,
                HotelId = tenantContext.HotelIds.FirstOrDefault(),
                ToolName = toolName,
                ArgumentsJsonMasked = MaskSensitiveData(argumentsJson),
                ResultCode = resultCode,
                DurationMs = durationMs,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.InsertToolAuditAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write tool audit log");
        }
    }

    public async Task LogChatAsync(
        TenantContext tenantContext,
        string conversationId,
        int questionLength,
        int citationCount,
        bool hitKnowledgeBase,
        int durationMs)
    {
        try
        {
            var entity = new ChatAuditLogEntity
            {
                TenantId = tenantContext.TenantId,
                UserId = tenantContext.UserId,
                ConversationId = conversationId,
                QuestionLength = questionLength,
                CitationCount = citationCount,
                HitKnowledgeBase = hitKnowledgeBase,
                DurationMs = durationMs,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.InsertChatAuditAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write chat audit log");
        }
    }

    private static string MaskSensitiveData(string json)
    {
        // Simple masking: replace phone numbers
        return System.Text.RegularExpressions.Regex.Replace(
            json,
            @"""phone""\s*:\s*""[^""]*""",
            @"""phone"":""***""");
    }
}
