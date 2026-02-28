using PmsAI.Api.Db.Entities;
using PmsAI.Api.Db.Repositories;
using PmsAI.Api.Models;

namespace PmsAI.Api.Services;

public class PendingActionService
{
    private readonly IPendingActionRepository _repository;
    private readonly ILogger<PendingActionService> _logger;
    private static readonly TimeSpan ExpirationTime = TimeSpan.FromMinutes(10);

    public PendingActionService(IPendingActionRepository repository, ILogger<PendingActionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PendingActionEntity> CreateAsync(
        TenantContext tenantContext,
        Guid hotelId,
        string conversationId,
        string toolName,
        string argumentsJson,
        string summary,
        CancellationToken cancellationToken = default)
    {
        var entity = new PendingActionEntity
        {
            PendingActionId = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            UserId = tenantContext.UserId,
            HotelId = hotelId,
            ConversationId = conversationId,
            ToolName = toolName,
            ArgumentsJson = argumentsJson,
            Summary = summary,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.Add(ExpirationTime),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(entity);
        _logger.LogInformation("Created pending action {Id} for tool {Tool}", entity.PendingActionId, toolName);
        return entity;
    }

    public async Task<PendingActionEntity?> ConfirmAsync(Guid pendingActionId, TenantContext tenantContext, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(pendingActionId);
        if (entity == null) return null;

        // Security: validate ownership
        if (entity.TenantId != tenantContext.TenantId || entity.UserId != tenantContext.UserId)
        {
            _logger.LogWarning("Unauthorized pending action access: {Id}", pendingActionId);
            return null;
        }

        if (entity.Status != "Pending" || entity.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogDebug("Pending action {Id} is expired or not pending: {Status}", pendingActionId, entity.Status);
            return null;
        }

        entity.Status = "Confirmed";
        await _repository.UpdateAsync(entity);
        return entity;
    }

    public async Task<bool> CancelAsync(Guid pendingActionId, TenantContext tenantContext, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(pendingActionId);
        if (entity == null) return false;

        if (entity.TenantId != tenantContext.TenantId || entity.UserId != tenantContext.UserId)
        {
            return false;
        }

        entity.Status = "Cancelled";
        await _repository.UpdateAsync(entity);
        return true;
    }
}
