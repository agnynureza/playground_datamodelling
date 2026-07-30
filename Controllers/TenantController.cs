using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TenantService.Api.Models;
using TenantService.Api.Services.Tenant;
using TenantService.Api.Services.Authenticate;
using TenantService.Api.Middleware;

namespace TenantService.Api.Controllers;

[ApiController]
[Route("api/tenant")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IAuthService _authService;

    public TenantController(ITenantService tenantService, IAuthService authService)
    {
        _tenantService = tenantService;
        _authService = authService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<CreateTenantResponse>> GetCurrentTenant()
    {
        var tenantId = HttpContext.GetTenantId();

        if (tenantId is null)
            return Unauthorized(new { error = "Invalid token" });

        var tenant = await _tenantService.GetTenantById(tenantId.Value);

        if (tenant is null)
            return NotFound(new { error = "Tenant not found" });

        return Ok(tenant);
    }

    [HttpPost]
    public async Task<ActionResult<CreateTenantResponse>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            var result = await _tenantService.CreateTenant(request);
            return CreatedAtAction(nameof(CreateTenant), new { id = result.TenantId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.Login(request);
        if (result is null)
            return Unauthorized(new { error = "Wrong tenant ID or password" });

        return Ok(result);
    }
}
