namespace TenantService.Api.Models;

public class TenantLimit
{
    public Guid TenantId { get; set; }
    public int RequestsPerWindow { get; set; }
    public int WindowSizeSeconds { get; set; }
    public string? Tier { get; set; }
}
