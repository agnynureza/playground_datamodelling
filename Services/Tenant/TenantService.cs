using TenantService.Api.Common;
using Entities = TenantService.Api.Entities;
using TenantService.Api.Models;
using TenantService.Api.Repositories;

namespace TenantService.Api.Services.Tenant;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _repository;

    public TenantService(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateTenantResponse> CreateTenant(CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.name))
            throw new ArgumentException("Tenant name is required.");

        if (string.IsNullOrWhiteSpace(request.password) || request.password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var tenant = new Entities.Tenant
        {
            TenantId = Guid.NewGuid(),
            Name = request.name.Trim(),
            ApiKey = PasswordHelper.GenerateApiKey(),
            Password = PasswordHelper.Hash(request.password),
            Status = "active",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddTenant(tenant);

        return new CreateTenantResponse
        {
            TenantId = tenant.TenantId,
            Name = tenant.Name,
            Status = tenant.Status,
            ApiKey = tenant.ApiKey,
            CreatedAtUtc = tenant.CreatedAtUtc
        };
    }

    public async Task<CreateTenantResponse?> GetTenantById(Guid tenantId)
    {
        var tenant = await _repository.GetById(tenantId);
        if (tenant is null)
            return null;

        return new CreateTenantResponse
        {
            TenantId = tenant.TenantId,
            Name = tenant.Name,
            ApiKey = tenant.ApiKey,
            Status = tenant.Status,
            CreatedAtUtc = tenant.CreatedAtUtc
        };
    }
}