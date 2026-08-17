using TenantService.Api.Models;

namespace TenantService.Api.Services.Tenant;

public interface ITenantLimitService
{
    Task<TenantLimit> GetLimit(Guid? tenantId, CancellationToken cancellationToken = default);
}