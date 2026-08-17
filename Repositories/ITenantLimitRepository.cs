using TenantService.Api.Models;

namespace TenantService.Api.Repositories;

public interface ITenantLimitRepository
{
    Task<TenantLimit?> GetByTenantId(Guid tenantId);
}
