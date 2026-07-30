using Microsoft.Data.SqlClient;
using TenantService.Api.Entities;

namespace TenantService.Api.Repositories;

// the inteface is implemented by the repository class, which is responsible for interacting with the database
public class TenantRepository : ITenantRepository
{
    private readonly string _connectionString;

    public TenantRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing 'SqlServer' connection string.");
    }

    public async Task AddTenant(Tenant tenant)
    {
        const string sql = """
            INSERT INTO P_Tenants (TenantId, Name, ApiKey, Password, Status, CreatedAtUtc)
            VALUES (@TenantId, @Name, @ApiKey, @Password, @Status, @CreatedAtUtc);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@TenantId", System.Data.SqlDbType.UniqueIdentifier).Value = tenant.TenantId;
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 200).Value = tenant.Name;
        command.Parameters.Add("@ApiKey", System.Data.SqlDbType.NVarChar, 200).Value = tenant.ApiKey;
        command.Parameters.Add("@Password", System.Data.SqlDbType.NVarChar, 266).Value = tenant.Password;
        command.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar, 20).Value = tenant.Status;
        command.Parameters.Add("@CreatedAtUtc", System.Data.SqlDbType.DateTime2).Value = tenant.CreatedAtUtc;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Tenant?> GetById(Guid tenantId)
    {
        const string sql = """
            SELECT TenantId, Name, ApiKey, Password, Status, CreatedAtUtc
            FROM P_Tenants
            WHERE TenantId = @TenantId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TenantId", System.Data.SqlDbType.UniqueIdentifier).Value = tenantId;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ? Map(reader) : null;
    }

    private static Tenant Map(SqlDataReader reader) => new()
    {
        TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        ApiKey = reader.GetString(reader.GetOrdinal("ApiKey")),
        Password = reader.GetString(reader.GetOrdinal("Password")),
        Status = reader.GetString(reader.GetOrdinal("Status")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc"))
    };
}
