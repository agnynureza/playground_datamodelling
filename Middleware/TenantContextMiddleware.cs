using System.Security.Claims;
using TenantService.Api.Common;
using TenantService.Api.Services.Tenant;
using TenantService.Api.Models;

namespace TenantService.Api.Middleware;

public static class TenantContextItems
{
    public const string TenantId = "TenantId";
}

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantLimitService _limit;

    public TenantContextMiddleware(RequestDelegate next, ITenantLimitService limit)
    {
        _next = next;
        _limit = limit;
    }

    public async Task Invoke(HttpContext context)
    {
        if (GetTenantIdFromClaims(context.User, out var tenantId))
        {
            context.Items[TenantContextItems.TenantId] = tenantId;

            var tenantLimit = await _limit.GetLimit(tenantId, context.RequestAborted);
            context.Items["TenantLimit"] = tenantLimit;
        }
        else if (HasBearerToken(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
            return;
        }

        await _next(context);
    }

    private static bool GetTenantIdFromClaims(ClaimsPrincipal principal, out Guid tenantId)
    {
        tenantId = Guid.Empty;

        var tenantIdValue = principal.GetTenantId();
        if (tenantIdValue is null || tenantIdValue == Guid.Empty)
            return false;

        tenantId = tenantIdValue.Value;
        return true;
    }

    private static bool HasBearerToken(HttpContext context)
    {
        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        return authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }
}

public static class HttpContextTenantExtensions
{
    public static Guid? GetTenantId(this HttpContext context)
    {
        return context.Items.TryGetValue(TenantContextItems.TenantId, out var value) && value is Guid tenantId
            ? tenantId
            : null;
    }
}