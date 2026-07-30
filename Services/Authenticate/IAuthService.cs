using TenantService.Api.Models;

namespace TenantService.Api.Services.Authenticate;

public interface IAuthService
{
    Task<LoginResponse?> Login(LoginRequest request);
}
