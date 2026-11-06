using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using RealTimeProcessingSystem.Models;

namespace RealTimeProcessingSystem.Services;

public sealed class FarmStorageService
{
    private readonly BlobServiceClient _blobService;
    private readonly TableClient _sensorReadings;
    private readonly string _containerName;

    public FarmStorageService(TokenCredential credential, IConfiguration configuration)
    {
        _containerName = configuration["FarmStorage:container"] ?? "drone-images";
        var storageConnection = configuration["FarmStorage"];
        if (!string.IsNullOrWhiteSpace(storageConnection))
        {
            _blobService = new BlobServiceClient(storageConnection);
            _sensorReadings = new TableServiceClient(storageConnection).GetTableClient("SensorReadings");
        }
        else
        {
            var blobEndpoint = configuration["FarmStorage:blobServiceUri"]
                ?? throw new InvalidOperationException("FarmStorage__blobServiceUri must be configured.");
            var tableEndpoint = configuration["FarmStorage:tableServiceUri"]
                ?? throw new InvalidOperationException("FarmStorage__tableServiceUri must be configured.");
            _blobService = new BlobServiceClient(new Uri(blobEndpoint), credential);
            _sensorReadings = new TableServiceClient(new Uri(tableEndpoint), credential).GetTableClient("SensorReadings");
        }
    }

    public async Task<string> UploadDroneImageAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        var blobName = $"{DateTimeOffset.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}{extension}";
        var container = _blobService.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            },
            cancellationToken);
        return blobName;
    }

    public async Task AddSampleReadingAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var table = _sensorReadings;
        await table.CreateIfNotExistsAsync(cancellationToken);
        var entity = new SensorReadingEntity
        {
            PartitionKey = "farm",
            RowKey = $"{DateTime.MaxValue.Ticks - now.UtcDateTime.Ticks:D19}-{Guid.NewGuid():N}",
            DeviceId = "SENSOR-001",
            SoilMoisturePercent = Math.Round(Random.Shared.NextDouble() * 65 + 20, 1),
            TemperatureC = Math.Round(Random.Shared.NextDouble() * 18 + 12, 1),
            HumidityPercent = Math.Round(Random.Shared.NextDouble() * 55 + 30, 1),
            IrrigationState = "Idle"
        };
        await table.AddEntityAsync(entity, cancellationToken);
    }
}
