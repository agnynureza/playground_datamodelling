namespace TenantService.Api.Configuration;

public class RateLimitSettings
{
    public int DefaultRequestsPerWindow { get; set; } = 60;
    public int DefaultWindowSizeSeconds { get; set; } = 60;
    public int TenantLimitCacheMinutes { get; set; } = 5;
}
