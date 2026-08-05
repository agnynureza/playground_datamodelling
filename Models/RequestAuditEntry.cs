namespace TenantService.Api.Models;

public sealed record RequestAuditEntry(
    Guid Id,
    DateTime TimestampUtc,
    Guid? TenantId,
    string? Username,
    string Method,
    string Path,
    string? RequestBody,
    string? ResponseBody,
    int StatusCode,
    int DurationMs);