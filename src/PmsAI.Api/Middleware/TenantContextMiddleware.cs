using System.Security.Claims;
using PmsAI.Api.Models;

namespace PmsAI.Api.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantContext = ParseTenantContext(context);
        context.Items["TenantContext"] = tenantContext;

        await _next(context);
    }

    private TenantContext ParseTenantContext(HttpContext context)
    {
        var user = context.User;
        var tc = new TenantContext();

        // TenantId: JWT claim "tenant_id" > Header "X-Tenant-Id"
        var tenantIdStr = user.FindFirstValue("tenant_id")
            ?? context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (Guid.TryParse(tenantIdStr, out var tenantId))
            tc.TenantId = tenantId;

        // UserId: JWT claim "sub"
        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        if (Guid.TryParse(userIdStr, out var userId))
            tc.UserId = userId;

        // HotelIds: JWT claim "hotel_ids" (comma-separated) > Header "X-Hotel-Ids"
        var hotelIdsStr = user.FindFirstValue("hotel_ids")
            ?? context.Request.Headers["X-Hotel-Ids"].FirstOrDefault();
        if (!string.IsNullOrEmpty(hotelIdsStr))
        {
            tc.HotelIds = hotelIdsStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => Guid.TryParse(s, out _))
                .Select(Guid.Parse)
                .ToList();
        }

        // Roles: JWT claim "roles" (comma-separated) > Header "X-Roles"
        var rolesStr = user.FindFirstValue("roles")
            ?? context.Request.Headers["X-Roles"].FirstOrDefault();
        if (!string.IsNullOrEmpty(rolesStr))
        {
            tc.Roles = rolesStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();
        }

        _logger.LogDebug("TenantContext: TenantId={TenantId}, UserId={UserId}, Hotels={Hotels}",
            tc.TenantId, tc.UserId, tc.HotelIds.Count);

        return tc;
    }
}
