using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RealTimeProcessing.Functions.Data;
using RealTimeProcessing.Functions.Models;

namespace RealTimeProcessing.Functions.Functions;

public sealed class QueueIrrigationCommandFunction
{
    private readonly FarmSqlRepository _devices;
    private readonly ILogger<QueueIrrigationCommandFunction> _logger;

    public QueueIrrigationCommandFunction(FarmSqlRepository devices, ILogger<QueueIrrigationCommandFunction> logger)
    {
        _devices = devices;
        _logger = logger;
    }

    [Function(nameof(ProcessIrrigationCommand))]
    public async Task ProcessIrrigationCommand(
        [QueueTrigger("irrigation-commands", Connection = "FarmStorage")] string queueMessage,
        FunctionContext context)
    {
        IrrigationCommandMessage? command;
        try
        {
            command = JsonSerializer.Deserialize<IrrigationCommandMessage>(
                queueMessage,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Queue message was not valid irrigation-command JSON.");
            throw;
        }

        if (command is null || string.IsNullOrWhiteSpace(command.DeviceId)
            || command.Command is not ("Start" or "Stop"))
        {
            throw new InvalidDataException("Queue message must include a deviceId and a Start or Stop command.");
        }

        await _devices.UpsertDeviceStatusAsync(command, context.CancellationToken);
        _logger.LogInformation(
            "Applied irrigation command {Command} to device {DeviceId}.",
            command.Command,
            command.DeviceId);
    }
}
