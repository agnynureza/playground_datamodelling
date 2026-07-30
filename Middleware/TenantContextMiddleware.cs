using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TenantService.Api.Middleware;

public static class TenantContextItems
{
    public const string TenantId = "TenantId";
}

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (GetTenantIdFromBearerToken(context, out var tenantId))
        {
            context.Items[TenantContextItems.TenantId] = tenantId;
        }
        else if (HasBearerToken(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
            return;
        }

        await _next(context);
    }

    private static bool GetTenantIdFromBearerToken(HttpContext context, out Guid tenantId)
    {
        tenantId = Guid.Empty;

        if (!HasBearerToken(context))
            return false;

        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        var token = authorizationHeader["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var tenantIdValue = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "tenantId")?.Value;
            return Guid.TryParse(tenantIdValue, out tenantId);
        }
        catch (ArgumentException)
        {
            return false;
        }
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