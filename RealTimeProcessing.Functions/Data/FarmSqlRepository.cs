using Microsoft.Data.SqlClient;
using RealTimeProcessing.Functions.Models;

namespace RealTimeProcessing.Functions.Data;

public sealed class FarmSqlRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public FarmSqlRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task UpsertDeviceStatusAsync(IrrigationCommandMessage command, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var sqlCommand = connection.CreateCommand();
        sqlCommand.CommandText = """
            MERGE dbo.Devices WITH (HOLDLOCK) AS target
            USING (VALUES (@DeviceId, @Command, @RequestedAtUtc))
                AS source (DeviceId, Command, RequestedAtUtc)
            ON target.DeviceId = source.DeviceId
            WHEN MATCHED THEN
                UPDATE SET
                    OperationalStatus = CASE
                        WHEN source.Command = N'Start' THEN N'Irrigating'
                        ELSE N'Online'
                    END,
                    LastCommand = source.Command,
                    LastCommandUtc = source.RequestedAtUtc,
                    UpdatedAtUtc = source.RequestedAtUtc
            WHEN NOT MATCHED THEN
                INSERT (DeviceId, OperationalStatus, LastCommand, LastCommandUtc)
                VALUES (
                    source.DeviceId,
                    CASE WHEN source.Command = N'Start' THEN N'Irrigating' ELSE N'Online' END,
                    source.Command,
                    source.RequestedAtUtc
                );
            """;

        sqlCommand.Parameters.Add(new SqlParameter("@DeviceId", System.Data.SqlDbType.NVarChar, 100) { Value = command.DeviceId });
        sqlCommand.Parameters.Add(new SqlParameter("@Command", System.Data.SqlDbType.NVarChar, 20) { Value = command.Command });
        sqlCommand.Parameters.Add(new SqlParameter("@RequestedAtUtc", System.Data.SqlDbType.DateTime2) { Value = command.RequestedAtUtc.UtcDateTime });
        await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
