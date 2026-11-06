using Azure.Core;
using Microsoft.Data.SqlClient;

namespace RealTimeProcessing.Functions.Data;

public sealed class SqlConnectionFactory
{
    private const string SqlScope = "https://database.windows.net/.default";
    private readonly TokenCredential _credential;
    private readonly string _connectionString;

    public SqlConnectionFactory(TokenCredential credential)
    {
        _credential = credential;
        _connectionString = Environment.GetEnvironmentVariable("SqlConnectionString")
            ?? throw new InvalidOperationException("The SqlConnectionString application setting is required.");
    }

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(
            new TokenRequestContext([SqlScope]),
            cancellationToken);

        var connection = new SqlConnection(_connectionString)
        {
            AccessToken = token.Token
        };

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
