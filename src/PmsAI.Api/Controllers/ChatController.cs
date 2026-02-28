using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmsAI.Api.Models;
using PmsAI.Api.Services;
using PmsAI.Contracts.Chat;

namespace PmsAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatOrchestrator _orchestrator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatOrchestrator orchestrator, ILogger<ChatController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "message is required" });

        var tenantContext = HttpContext.Items["TenantContext"] as TenantContext;
        if (tenantContext == null || tenantContext.TenantId == Guid.Empty)
            return Unauthorized(new { error = "Invalid tenant context" });

        try
        {
            var response = await _orchestrator.ChatAsync(request, tenantContext, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat failed for tenant {TenantId}", tenantContext.TenantId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
