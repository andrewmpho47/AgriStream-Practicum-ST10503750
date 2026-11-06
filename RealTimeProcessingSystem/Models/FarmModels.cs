using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace RealTimeProcessingSystem.Models;

public sealed class FarmDashboardViewModel
{
    public IReadOnlyList<SensorReadingViewModel> SensorReadings { get; init; } = [];
    public string? Error { get; init; }
    public string? Notice { get; init; }
    public string? UserName { get; init; }
}

public sealed class SensorReadingViewModel
{
    public string DeviceId { get; init; } = string.Empty;
    public double SoilMoisturePercent { get; init; }
    public double TemperatureC { get; init; }
    public double HumidityPercent { get; init; }
    public string IrrigationState { get; init; } = "Idle";
    public DateTimeOffset? Timestamp { get; init; }
}

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

public sealed class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed class LoginViewModel
{
    [Required, StringLength(50)]
    public string Username { get; init; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;
}

public sealed record FarmUserSummary(string Username, string SystemRole);

public sealed class ChangeRoleViewModel
{
    [Required, StringLength(50)]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string SystemRole { get; init; } = string.Empty;
}
