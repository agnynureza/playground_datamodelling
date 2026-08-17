using Microsoft.Data.SqlClient;
using TenantService.Api.Models;

namespace TenantService.Api.Repositories;

public class TenantLimitRepository : ITenantLimitRepository
{
    private readonly string _connectionString;

    public TenantLimitRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing 'SqlServer' connection string.");
    }

    public async Task<TenantLimit?> GetByTenantId(Guid tenantId)
    {
        const string sql = @"SELECT TenantId, RequestsPerWindow, WindowSizeSeconds, Tier FROM P_TenantLimits WHERE TenantId = @TenantId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TenantId", System.Data.SqlDbType.UniqueIdentifier).Value = tenantId;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new TenantLimit
        {
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            RequestsPerWindow = reader.GetInt32(reader.GetOrdinal("RequestsPerWindow")),
            WindowSizeSeconds = reader.GetInt32(reader.GetOrdinal("WindowSizeSeconds")),
            Tier = reader.IsDBNull(reader.GetOrdinal("Tier")) ? null : reader.GetString(reader.GetOrdinal("Tier"))
        };
    }
}
