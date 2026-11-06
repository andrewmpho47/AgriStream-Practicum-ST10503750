using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace RealTimeProcessingSystem.Services;

public sealed class FarmSqlConnectionFactory
{
    private const string SqlScope = "https://database.windows.net/.default";
    private readonly TokenCredential _credential;
    private readonly string _connectionString;

    public FarmSqlConnectionFactory(TokenCredential credential, IConfiguration configuration)
    {
        _credential = credential;
        _connectionString = configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("The SqlConnectionString application setting is required.");
    }

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext([SqlScope]), cancellationToken);
        var connection = new SqlConnection(_connectionString) { AccessToken = token.Token };
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
