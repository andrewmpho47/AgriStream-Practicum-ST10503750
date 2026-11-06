using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using RealTimeProcessingSystem.Models;

namespace RealTimeProcessingSystem.Services;

public sealed class FarmUserService
{
    private const int Iterations = 210_000;
    private readonly FarmSqlConnectionFactory _connectionFactory;

    public FarmUserService(FarmSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> RegisterFarmerAsync(string username, string password, CancellationToken cancellationToken)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        var storedHash = $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.FarmUsers (Username, PasswordHash, SystemRole)
            VALUES (@Username, @PasswordHash, N'Farmer');
            """;
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = username.Trim() });
        command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = storedHash });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public async Task<FarmUserSummary?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT PasswordHash, SystemRole FROM dbo.FarmUsers WHERE Username = @Username;";
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = username.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var storedHash = reader.GetString(0);
        var role = reader.GetString(1);
        return VerifyPassword(password, storedHash)
            ? new FarmUserSummary(username.Trim(), role)
            : null;
    }

    public async Task<IReadOnlyList<FarmUserSummary>> GetUsersAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Username, SystemRole FROM dbo.FarmUsers ORDER BY Username;";

        var users = new List<FarmUserSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new FarmUserSummary(reader.GetString(0), reader.GetString(1)));
        }

        return users;
    }

    public async Task<bool> ChangeRoleAsync(string username, string role, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.FarmUsers SET SystemRole = @SystemRole WHERE Username = @Username;";
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = username.Trim() });
        command.Parameters.Add(new SqlParameter("@SystemRole", SqlDbType.NVarChar, 20) { Value = role });
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4
            || parts[0] != "PBKDF2-SHA256"
            || !int.TryParse(parts[1], out var iterations)
            || iterations is < 100_000 or > 1_000_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length < 16 || expected.Length != 32)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            return false;
        }
    }
}
