using System.Security.Cryptography;
using System.Text;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RealTimeProcessing.Functions.Models;

namespace RealTimeProcessing.Functions.Functions;

public sealed class RecordDroneImageUploadFunction
{
    private readonly TableClient _processingLog;
    private readonly ILogger<RecordDroneImageUploadFunction> _logger;

    public RecordDroneImageUploadFunction(TableClient processingLog, ILogger<RecordDroneImageUploadFunction> logger)
    {
        _processingLog = processingLog;
        _logger = logger;
    }

    [Function(nameof(RecordDroneImageUpload))]
    public async Task RecordDroneImageUpload(
        [BlobTrigger("drone-images/{name}", Connection = "FarmStorage")] Stream image,
        string name,
        FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;
        await _processingLog.CreateIfNotExistsAsync(cancellationToken);
        var rowKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name))).ToLowerInvariant();
        var entity = new BlobProcessingLogEntity
        {
            RowKey = rowKey,
            BlobName = name,
            SizeBytes = image.CanSeek ? image.Length : -1,
            ProcessingStatus = "Processed",
            ProcessedAtUtc = DateTimeOffset.UtcNow
        };

        await _processingLog.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        _logger.LogInformation("Logged drone image {BlobName} ({SizeBytes} bytes) to Table Storage.", name, entity.SizeBytes);
    }
}
