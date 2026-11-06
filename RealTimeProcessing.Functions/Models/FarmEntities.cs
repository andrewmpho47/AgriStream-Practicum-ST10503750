using Azure;
using Azure.Data.Tables;

namespace RealTimeProcessing.Functions.Models;

public sealed class SensorReadingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public double SoilMoisturePercent { get; set; }
    public double TemperatureC { get; set; }
    public double HumidityPercent { get; set; }
    public string IrrigationState { get; set; } = "Idle";
}

public sealed class BlobProcessingLogEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "drone-image";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ProcessingStatus { get; set; } = "Processed";
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

public sealed class IrrigationCommandRequest
{
    public string? DeviceId { get; init; }
    public string? Command { get; init; }
}

public sealed record IrrigationCommandMessage(string DeviceId, string Command, DateTimeOffset RequestedAtUtc);
