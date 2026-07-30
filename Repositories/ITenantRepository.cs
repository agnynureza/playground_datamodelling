using TenantService.Api.Entities;

namespace TenantService.Api.Repositories;

public interface ITenantRepository
{
    Task AddTenant(Tenant tenant);
    Task<Tenant?> GetById(Guid tenantId);
}
