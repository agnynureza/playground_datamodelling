using Microsoft.Extensions.Caching.Memory;
using TenantService.Api.Configuration;
using TenantService.Api.Models;
using TenantService.Api.Repositories;

namespace TenantService.Api.Services.Tenant;

public sealed class TenantLimitService : ITenantLimitService
{
    private readonly ITenantLimitRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly RateLimitSettings _defaults;

    public TenantLimitService(
        ITenantLimitRepository repository,
        IMemoryCache cache,
        Microsoft.Extensions.Options.IOptions<RateLimitSettings> defaults)
    {
        _repository = repository;
        _cache = cache;
        _defaults = defaults?.Value ?? new RateLimitSettings();
    }

    public async Task<TenantLimit> GetLimit(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return CreateDefaultLimit();
        }

        var cacheKey = $"tenant-limit:{tenantId.Value}";
        if (_cache.TryGetValue(cacheKey, out TenantLimit? cached) && cached is not null)
        {
            return cached;
        }

        var resolved = await _repository.GetByTenantId(tenantId.Value);
        var limit = resolved ?? CreateDefaultLimit(tenantId.Value);

        _cache.Set(cacheKey, limit, TimeSpan.FromMinutes(Math.Max(1, _defaults.TenantLimitCacheMinutes)));
        return limit;
    }

    private TenantLimit CreateDefaultLimit(Guid? tenantId = null)
    {
        return new TenantLimit
        {
            TenantId = tenantId ?? Guid.Empty,
            RequestsPerWindow = _defaults.DefaultRequestsPerWindow,
            WindowSizeSeconds = _defaults.DefaultWindowSizeSeconds
        };
    }
}