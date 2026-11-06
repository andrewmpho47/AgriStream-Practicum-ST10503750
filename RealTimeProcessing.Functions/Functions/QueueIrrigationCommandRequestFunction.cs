using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RealTimeProcessing.Functions.Models;

namespace RealTimeProcessing.Functions.Functions;

public sealed class QueueIrrigationCommandRequestFunction
{
    private readonly QueueClient _irrigationQueue;
    private readonly ILogger<QueueIrrigationCommandRequestFunction> _logger;

    public QueueIrrigationCommandRequestFunction(
        QueueServiceClient queues,
        ILogger<QueueIrrigationCommandRequestFunction> logger)
    {
        _irrigationQueue = queues.GetQueueClient("irrigation-commands");
        _logger = logger;
    }

    [Function(nameof(RequestIrrigation))]
    public async Task<HttpResponseData> RequestIrrigation(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "irrigation")] HttpRequestData request,
        FunctionContext context)
    {
        IrrigationCommandRequest? input;
        try
        {
            input = await JsonSerializer.DeserializeAsync<IrrigationCommandRequest>(
                request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.CancellationToken);
        }
        catch (JsonException)
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "Request body must be valid JSON.");
        }

        if (input is null || string.IsNullOrWhiteSpace(input.DeviceId) || input.DeviceId.Trim().Length > 100)
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "deviceId is required and must be 100 characters or fewer.");
        }

        var commandName = input.Command?.Trim();
        if (commandName is not ("Start" or "Stop"))
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "command must be either Start or Stop.");
        }

        var command = new IrrigationCommandMessage(input.DeviceId.Trim(), commandName, DateTimeOffset.UtcNow);
        await _irrigationQueue.CreateIfNotExistsAsync(cancellationToken: context.CancellationToken);
        await _irrigationQueue.SendMessageAsync(
            JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Queued irrigation command {Command} for {DeviceId}.", command.Command, command.DeviceId);
        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { accepted = true, command.DeviceId, command.Command }, context.CancellationToken);
        return response;
    }

    private static async Task<HttpResponseData> WriteErrorAsync(
        HttpRequestData request,
        HttpStatusCode status,
        string message)
    {
        var response = request.CreateResponse(status);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
