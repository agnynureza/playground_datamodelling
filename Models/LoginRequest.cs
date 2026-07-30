namespace TenantService.Api.Models;

public class LoginRequest
{
    public Guid tenantID { get; set; } = Guid.Empty;
    public string password { get; set; } = string.Empty;
}
