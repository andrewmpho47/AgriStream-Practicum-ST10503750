using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using RealTimeProcessing.Functions.Models;

namespace RealTimeProcessing.Functions.Functions;

public sealed class GetSensorReadingsFunction
{
    private readonly TableClient _sensorReadings;

    public GetSensorReadingsFunction(TableServiceClient tableService)
    {
        _sensorReadings = tableService.GetTableClient("SensorReadings");
    }

    [Function(nameof(GetSensorReadings))]
    public async Task<HttpResponseData> GetSensorReadings(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "telemetry")] HttpRequestData request,
        FunctionContext context)
    {
        var readings = new List<SensorReadingEntity>();
        var filter = "PartitionKey eq 'farm'";
        await foreach (var reading in _sensorReadings.QueryAsync<SensorReadingEntity>(
            filter: filter,
            maxPerPage: 100,
            cancellationToken: context.CancellationToken))
        {
            readings.Add(reading);
            if (readings.Count == 100)
            {
                break;
            }
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            readings.OrderByDescending(x => x.Timestamp).Take(100).ToArray(),
            context.CancellationToken);
        return response;
    }
}
