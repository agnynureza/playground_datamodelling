namespace TenantService.Api.Entities;

// facing with the database, not the client
public class Tenant
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Status { get; set; } = "active"; // "active" | "suspended"

    public DateTime CreatedAtUtc { get; set; }
}
