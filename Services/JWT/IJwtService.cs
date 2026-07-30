namespace TenantService.Api.Services.Jwt;

public interface IJwtService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(Guid tenantId, string name, string status);
}
