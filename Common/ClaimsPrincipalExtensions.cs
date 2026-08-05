using System.Security.Claims;

namespace TenantService.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetTenantId(this ClaimsPrincipal principal)
    {
        var tenantIdValue = principal.FindFirstValue("tenantId");
        return Guid.TryParse(tenantIdValue, out var tenantId) ? tenantId : null;
    }

    public static string? GetTenantName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("name")
            ?? principal.FindFirstValue(ClaimTypes.Name);
    }
}