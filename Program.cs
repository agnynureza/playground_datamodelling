using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TenantService.Api.Configuration;
using TenantService.Api.Middleware;
using TenantService.Api.Models;
using TenantService.Api.Repositories;
using Svc = TenantService.Api.Services;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// JWT config
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

// Rate limit settings
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));

// repository = logic layer to interact with the database 
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddSingleton<IRequestAuditRepository, RequestAuditRepository>();
builder.Services.AddScoped<ITenantLimitRepository, TenantLimitRepository>();

// service = business logic layer
builder.Services.AddScoped<Svc.Tenant.ITenantLimitService, Svc.Tenant.TenantLimitService>();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<Svc.Tenant.ITenantService, Svc.Tenant.TenantService>();
builder.Services.AddScoped<Svc.Authenticate.IAuthService, Svc.Authenticate.AuthService>();
builder.Services.AddScoped<Svc.Jwt.IJwtService, Svc.Jwt.JwtService>();
builder.Services.AddSingleton<Svc.RequestAudit.IRequestAuditQueue, Svc.RequestAudit.RequestAuditQueue>();
builder.Services.AddHostedService<Svc.RequestAudit.RequestAuditBackgroundService>();

// Set up JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfter = TimeSpan.FromSeconds(60);
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra))
        {
            retryAfter = ra;
        }
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later.",
            retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds)
        }, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var defaults = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;
        var tenantId = httpContext.GetTenantId();
        var tenantLimit = httpContext.Items.TryGetValue("TenantLimit", out var value) && value is TenantLimit limit
            ? limit : null;

        var requestsPerWindow = tenantLimit?.RequestsPerWindow ?? defaults.DefaultRequestsPerWindow;
        var windowSizeSeconds = tenantLimit?.WindowSizeSeconds ?? defaults.DefaultWindowSizeSeconds;

        var partitionKey = tenantId?.ToString()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{partitionKey}:{requestsPerWindow}:{windowSizeSeconds}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = requestsPerWindow,
                Window = TimeSpan.FromSeconds(windowSizeSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<TenantContextMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "server up" }))
    .DisableRateLimiting();

app.Run();
