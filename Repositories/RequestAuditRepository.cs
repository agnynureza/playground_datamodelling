using Microsoft.Data.SqlClient;
using TenantService.Api.Models;

namespace TenantService.Api.Repositories;

public class RequestAuditRepository : IRequestAuditRepository
{
    private readonly string _connectionString;

    public RequestAuditRepository(IConfiguration configuration){
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing 'SqlServer' connection string.");
    }

    public async Task InsertAsync(RequestAuditEntry entry, CancellationToken cancellationToken = default){
        // dont forget change table name remove "P_ prefix"
        const string sql = """
            INSERT INTO P_Logs (
                Id, TimestampUTC,
                TenantId, Username,
                Method, Path,
                RequestBody, ResponseBody,
                StatusCode, DurationMs)
            VALUES (
                @Id, @TimestampUTC,
                @TenantId, @Username,
                @Method, @Path,
                @RequestBody, @ResponseBody,
                @StatusCode, @DurationMs);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier).Value = entry.Id;
        command.Parameters.Add("@TimestampUTC", System.Data.SqlDbType.DateTime2).Value = entry.TimestampUtc;
        command.Parameters.Add("@TenantId", System.Data.SqlDbType.UniqueIdentifier).Value = (object?)entry.TenantId ?? DBNull.Value;
        command.Parameters.Add("@Username", System.Data.SqlDbType.NVarChar, 256).Value = (object?)entry.Username ?? DBNull.Value;
        command.Parameters.Add("@Method", System.Data.SqlDbType.NVarChar, 10).Value = entry.Method;
        command.Parameters.Add("@Path", System.Data.SqlDbType.NVarChar, 500).Value = entry.Path;
        command.Parameters.Add("@RequestBody", System.Data.SqlDbType.NVarChar, -1).Value = (object?)entry.RequestBody ?? DBNull.Value;
        command.Parameters.Add("@ResponseBody", System.Data.SqlDbType.NVarChar, -1).Value = (object?)entry.ResponseBody ?? DBNull.Value;
        command.Parameters.Add("@StatusCode", System.Data.SqlDbType.Int).Value = entry.StatusCode;
        command.Parameters.Add("@DurationMs", System.Data.SqlDbType.Int).Value = entry.DurationMs;

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}