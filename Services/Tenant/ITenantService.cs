using TenantService.Api.Models;

namespace TenantService.Api.Services.Tenant;

public interface ITenantService
{
    Task<CreateTenantResponse> CreateTenant(CreateTenantRequest request);
    Task<CreateTenantResponse?> GetTenantById(Guid tenantId);
}
