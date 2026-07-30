namespace TenantService.Api.Models;

// facing with the client, DTO
public class CreateTenantRequest
{
    public string name { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}
