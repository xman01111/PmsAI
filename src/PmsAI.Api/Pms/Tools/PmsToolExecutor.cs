using System.Text.Json;
using PmsAI.Api.Db.Entities;
using PmsAI.Api.Db.Repositories;
using PmsAI.Api.Models;
using PmsAI.Api.Services;
using PmsAI.Contracts.Pms;

namespace PmsAI.Api.Pms.Tools;

public class PmsToolExecutor
{
    private readonly IPmsClient _pmsClient;
    private readonly HotelResolverService _hotelResolver;
    private readonly PendingActionService _pendingActionService;
    private readonly AuditService _auditService;
    private readonly ILogger<PmsToolExecutor> _logger;

    public PmsToolExecutor(
        IPmsClient pmsClient,
        HotelResolverService hotelResolver,
        PendingActionService pendingActionService,
        AuditService auditService,
        ILogger<PmsToolExecutor> logger)
    {
        _pmsClient = pmsClient;
        _hotelResolver = hotelResolver;
        _pendingActionService = pendingActionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        string argumentsJson,
        TenantContext tenantContext,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        if (PmsToolDefinitions.WritableTools.Contains(toolName))
        {
            return await HandleWriteToolAsync(toolName, argumentsJson, tenantContext, conversationId, cancellationToken);
        }

        if (PmsToolDefinitions.ReadableTools.Contains(toolName))
        {
            return await HandleReadToolAsync(toolName, argumentsJson, tenantContext, cancellationToken);
        }

        return ToolExecutionResult.Error($"未知工具: {toolName}");
    }

    private async Task<ToolExecutionResult> HandleReadToolAsync(
        string toolName,
        string argumentsJson,
        TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string resultCode = "OK";
        string resultJson = string.Empty;

        try
        {
            using var args = JsonDocument.Parse(argumentsJson);
            var hotelRef = args.RootElement.TryGetProperty("hotelRef", out var hotelRefEl) ? hotelRefEl.GetString() : null;
            var hotelId = await _hotelResolver.ResolveAsync(tenantContext, hotelRef, cancellationToken);

            if (hotelId == null)
            {
                return ToolExecutionResult.Error("无法确定酒店，请指定酒店名称或编号。");
            }

            object result = toolName switch
            {
                "pms_get_reservations" => await _pmsClient.GetReservationsAsync(hotelId.Value, ParseReservationQuery(args.RootElement), cancellationToken),
                "pms_get_room_status" => await _pmsClient.GetRoomStatusAsync(hotelId.Value, ParseRoomStatusQuery(args.RootElement), cancellationToken),
                _ => throw new InvalidOperationException($"Unknown read tool: {toolName}")
            };

            resultJson = JsonSerializer.Serialize(result);
            return ToolExecutionResult.Success(resultJson);
        }
        catch (Exception ex)
        {
            resultCode = "ERROR";
            _logger.LogError(ex, "Tool execution failed: {Tool}", toolName);
            return ToolExecutionResult.Error(ex.Message);
        }
        finally
        {
            sw.Stop();
            await _auditService.LogToolCallAsync(tenantContext, toolName, argumentsJson, resultCode, (int)sw.ElapsedMilliseconds);
        }
    }

    private async Task<ToolExecutionResult> HandleWriteToolAsync(
        string toolName,
        string argumentsJson,
        TenantContext tenantContext,
        string conversationId,
        CancellationToken cancellationToken)
    {
        using var args = JsonDocument.Parse(argumentsJson);
        var hotelRef = args.RootElement.TryGetProperty("hotelRef", out var hotelRefEl) ? hotelRefEl.GetString() : null;
        var hotelId = await _hotelResolver.ResolveAsync(tenantContext, hotelRef, cancellationToken);

        if (hotelId == null)
        {
            return ToolExecutionResult.Error("无法确定酒店，请指定酒店名称或编号。");
        }

        var summary = BuildWriteSummary(toolName, args.RootElement);
        var pendingAction = await _pendingActionService.CreateAsync(
            tenantContext, hotelId.Value, conversationId, toolName, argumentsJson, summary, cancellationToken);

        return ToolExecutionResult.PendingConfirmation(pendingAction);
    }

    public async Task<ToolExecutionResult> ExecuteConfirmedAsync(
        Guid pendingActionId,
        TenantContext tenantContext,
        CancellationToken cancellationToken = default)
    {
        var pendingAction = await _pendingActionService.ConfirmAsync(pendingActionId, tenantContext, cancellationToken);
        if (pendingAction == null)
        {
            return ToolExecutionResult.Error("操作不存在、已过期或无权限。");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        string resultCode = "OK";

        try
        {
            using var args = JsonDocument.Parse(pendingAction.ArgumentsJson);

            object result = pendingAction.ToolName switch
            {
                "pms_create_work_order" => await _pmsClient.CreateWorkOrderAsync(
                    pendingAction.HotelId,
                    ParseCreateWorkOrder(args.RootElement),
                    cancellationToken),
                "pms_request_late_checkout" => await _pmsClient.RequestLateCheckoutAsync(
                    pendingAction.HotelId,
                    ParseLateCheckoutRequest(args.RootElement),
                    cancellationToken),
                _ => throw new InvalidOperationException($"Unknown write tool: {pendingAction.ToolName}")
            };

            return ToolExecutionResult.Success(JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            resultCode = "ERROR";
            _logger.LogError(ex, "Confirmed tool execution failed: {Tool}", pendingAction.ToolName);
            return ToolExecutionResult.Error(ex.Message);
        }
        finally
        {
            sw.Stop();
            await _auditService.LogToolCallAsync(tenantContext, pendingAction.ToolName, pendingAction.ArgumentsJson, resultCode, (int)sw.ElapsedMilliseconds);
        }
    }

    private static ReservationQueryDto ParseReservationQuery(JsonElement el) => new()
    {
        ReservationNo = GetString(el, "reservationNo"),
        GuestName = GetString(el, "guestName"),
        Phone = GetString(el, "phone"),
        ArrivalDate = GetString(el, "arrivalDate")
    };

    private static RoomStatusQueryDto ParseRoomStatusQuery(JsonElement el) => new()
    {
        Date = GetString(el, "date")
    };

    private static CreateWorkOrderDto ParseCreateWorkOrder(JsonElement el) => new()
    {
        RoomNo = GetString(el, "roomNo") ?? string.Empty,
        Category = GetString(el, "category") ?? string.Empty,
        Priority = GetString(el, "priority") ?? string.Empty,
        Description = GetString(el, "description") ?? string.Empty
    };

    private static LateCheckoutRequestDto ParseLateCheckoutRequest(JsonElement el) => new()
    {
        ReservationNo = GetString(el, "reservationNo") ?? string.Empty,
        RequestedCheckoutTime = GetString(el, "requestedCheckoutTime") ?? string.Empty,
        Reason = GetString(el, "reason")
    };

    private static string? GetString(JsonElement el, string key)
        => el.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static string Truncate(string? value, int maxLength)
        => value == null ? string.Empty : value.Length > maxLength ? value[..maxLength] + "..." : value;

    private static string BuildWriteSummary(string toolName, JsonElement args) => toolName switch
    {
        "pms_create_work_order" =>
            $"将为房间{GetString(args, "roomNo")}创建{GetString(args, "category")}工单，优先级：{GetString(args, "priority")}。描述：{Truncate(GetString(args, "description"), 50)}。是否确认？",
        "pms_request_late_checkout" =>
            $"将为预订单{GetString(args, "reservationNo")}申请延迟退房至{GetString(args, "requestedCheckoutTime")}。是否确认？",
        _ => $"确认执行操作 {toolName}？"
    };
}

public class ToolExecutionResult
{
    public bool IsSuccess { get; private set; }
    public bool IsPending { get; private set; }
    public string? ResultJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public PendingActionEntity? PendingAction { get; private set; }

    public static ToolExecutionResult Success(string resultJson) => new() { IsSuccess = true, ResultJson = resultJson };
    public static ToolExecutionResult Error(string message) => new() { IsSuccess = false, ErrorMessage = message };
    public static ToolExecutionResult PendingConfirmation(PendingActionEntity pending) => new() { IsPending = true, PendingAction = pending };
}
