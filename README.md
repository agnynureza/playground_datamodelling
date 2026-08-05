# TenantService.Api

## Status
Run `dotnet watch run` yourself first, for hot reload

## Folder structure
```
TenantService.Api/
├── Controllers/      → HTTP endpoints (thin, no business logic)
├── Models/            → client-facing DTOs
├── Entities/           → database-facing model (Tenant)
├── Repositories/       → raw ADO.NET data access (no EF Core)
├── Services/            → business logic (onboarding, auth, JWT issuance)
├── Configuration/        → strongly-typed settings (JwtSettings)
├── Common/                → cross-cutting helpers (PasswordHasher)
├── Scripts/                → manual SQL DDL (no migrations)
└── Program.cs               → composition root / DI wiring
```
No `Data/` folder or EF Core's `DbContext` lived there and has been removed. Connection handling now lives directly in
`TenantRepository` (opens a `SqlConnection` per call via `IConfiguration`).

## Setup
```bash
# from inside root project
dotnet restore
dotnet run
```
Before first run:
1. Point `ConnectionStrings:SqlServer` in `appsettings.json`
2. Run `Scripts/CreateTenantsTable.sql` against that database manually
3. example to add library: dotnet add package Microsoft.AspNetCore.Identity

Audit logging:
1. Run `Database/Schema/CreateLoggingTable.sql` against the same database
2. Every API request is captured in `P_Logs` with tenant id, username, request body, response body, status code, and duration
3. Sensitive fields are redacted and large bodies are truncated before storage

## Endpoints

### POST /api/tenants — create a tenant
```json
{ "name": "Acme Corp", "username": "acme_admin", "password": "at-least-8-chars" }
```
→ 201, body has `tenantId`, `name`, `username`, `status`, `createdAtUtc`.
No password in the response, ever — including your own hash.

### POST /api/tenants/login — exchange credentials for a JWT
```json
{ "username": "acme_admin", "password": "at-least-8-chars" }
```
→ 200 with `token` + `expiresAtUtc`, or 401 for wrong username, wrong
password, or a suspended tenant — all three return the same generic
error, deliberately, so a caller can't use the response to figure out
which one it was.

JWT payload: `tenantId`, `name`, `status`.