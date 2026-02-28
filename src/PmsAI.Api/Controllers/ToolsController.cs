using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmsAI.Api.Models;
using PmsAI.Api.Pms.Tools;

namespace PmsAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ToolsController : ControllerBase
{
    private readonly PmsToolExecutor _toolExecutor;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(PmsToolExecutor toolExecutor, ILogger<ToolsController> logger)
    {
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest request, CancellationToken cancellationToken)
    {
        var tenantContext = HttpContext.Items["TenantContext"] as TenantContext;
        if (tenantContext == null || tenantContext.TenantId == Guid.Empty)
            return Unauthorized(new { error = "Invalid tenant context" });

        if (!request.Confirm)
        {
            return Ok(new { success = false, answer = "操作已取消。" });
        }

        try
        {
            var result = await _toolExecutor.ExecuteConfirmedAsync(request.PendingActionId, tenantContext, cancellationToken);

            if (!result.IsSuccess)
            {
                return Ok(new { success = false, answer = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                answer = "操作已成功执行。",
                toolResult = result.ResultJson
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Confirm failed for pendingAction {Id}", request.PendingActionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class ConfirmRequest
{
    public Guid PendingActionId { get; set; }
    public bool Confirm { get; set; }
}
