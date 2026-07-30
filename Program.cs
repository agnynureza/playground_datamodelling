using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TenantService.Api.Configuration;
using TenantService.Api.Entities;
using TenantService.Api.Middleware;
using TenantService.Api.Repositories;
using Svc = TenantService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// JWT config
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

// repository = logic layer to interact with the database 
builder.Services.AddScoped<ITenantRepository, TenantRepository>();

// service = business logic layer
builder.Services.AddScoped<Svc.Tenant.ITenantService, Svc.Tenant.TenantService>();
builder.Services.AddScoped<Svc.Authenticate.IAuthService, Svc.Authenticate.AuthService>();
builder.Services.AddScoped<Svc.Jwt.IJwtService, Svc.Jwt.JwtService>();

// init JWT
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "server up" }));

app.Run();
