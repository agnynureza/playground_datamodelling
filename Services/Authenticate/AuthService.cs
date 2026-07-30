using TenantService.Api.Common;
using TenantService.Api.Models;
using TenantService.Api.Repositories;
using TenantService.Api.Services;

namespace TenantService.Api.Services.Authenticate;

public class AuthService : IAuthService
{
    private readonly ITenantRepository _repository;
    private readonly Jwt.IJwtService _jwtService;

    public AuthService(ITenantRepository repository, Jwt.IJwtService jwtService)
    {
        _repository = repository;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        if (request.tenantID == Guid.Empty || string.IsNullOrWhiteSpace(request.password))
            return null;

        var tenant = await _repository.GetById(request.tenantID);
        if (tenant is null)
            return null;

        if (!PasswordHelper.Verify(request.password, tenant.Password))
            return null;

        if (!string.Equals(tenant.Status, "active", StringComparison.OrdinalIgnoreCase))
            return null; // if tenant suspended or inactive, do not allow login

        var (token, expiresAtUtc) = _jwtService.GenerateToken(tenant.TenantId, tenant.Name, tenant.Status);

        return new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
