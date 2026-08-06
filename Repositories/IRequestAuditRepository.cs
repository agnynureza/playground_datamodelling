using TenantService.Api.Models;

namespace TenantService.Api.Repositories;

public interface IRequestAuditRepository
{
    Task InsertAsync(RequestAuditEntry entry, CancellationToken cancellationToken = default);
}