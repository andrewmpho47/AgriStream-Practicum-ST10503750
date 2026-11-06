using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RealTimeProcessingSystem.Models;
using RealTimeProcessingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Azure;

namespace RealTimeProcessingSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly FarmFunctionsClient _functions;
    private readonly FarmStorageService _storage;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        FarmFunctionsClient functions,
        FarmStorageService storage,
        ILogger<HomeController> logger)
    {
        _functions = functions;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (readings, error) = await GetReadingsAsync(cancellationToken);
        return View(new FarmDashboardViewModel
        {
            SensorReadings = readings,
            Error = error,
            Notice = TempData["Notice"] as string,
            UserName = User.Identity?.Name
        });
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        try
        {
            return Json(await _functions.GetSensorReadingsAsync(cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or RequestFailedException)
        {
            _logger.LogError(exception, "Could not retrieve telemetry from the Azure Functions API.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Telemetry service is unavailable. Check the Function App configuration and storage permissions." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile? image, CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0)
        {
            TempData["Notice"] = "Select a non-empty image file before uploading.";
            return RedirectToAction(nameof(Index));
        }

        if (image.Length > 10 * 1024 * 1024)
        {
            TempData["Notice"] = "The image must be 10 MB or smaller.";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")
            || image.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            TempData["Notice"] = "Upload a JPG, PNG, or WebP image.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var blobName = await _storage.UploadDroneImageAsync(image, extension, cancellationToken);
            TempData["Notice"] = $"Image uploaded as {blobName}. The Blob-triggered Function will log its processing status.";
        }
        catch (Exception exception) when (exception is RequestFailedException or InvalidOperationException or HttpRequestException)
        {
            _logger.LogError(exception, "Drone image upload failed.");
            TempData["Notice"] = "Image upload failed. Check the Blob endpoint and the App Service managed identity's storage role.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestIrrigation(string? deviceId, string? command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || command is not ("Start" or "Stop"))
        {
            TempData["Notice"] = "Enter a device ID and choose Start or Stop.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _functions.RequestIrrigationAsync(deviceId.Trim(), command, cancellationToken);
            TempData["Notice"] = $"The {command} irrigation command was queued for {deviceId.Trim()}.";
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            _logger.LogError(exception, "Could not queue irrigation command for device {DeviceId}.", deviceId);
            TempData["Notice"] = "The irrigation command could not be queued. Check the Function App URL, key, and Queue permissions.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateReading(CancellationToken cancellationToken)
    {
        try
        {
            await _storage.AddSampleReadingAsync(cancellationToken);
            TempData["Notice"] = "A clearly labeled sample sensor reading was added to Table Storage.";
        }
        catch (Exception exception) when (exception is RequestFailedException or InvalidOperationException or HttpRequestException)
        {
            _logger.LogError(exception, "Could not add a sample sensor reading.");
            TempData["Notice"] = "Sample reading failed. Check the Table endpoint and the App Service managed identity's storage role.";
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<(IReadOnlyList<SensorReadingViewModel> Readings, string? Error)> GetReadingsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return (await _functions.GetSensorReadingsAsync(cancellationToken), null);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or RequestFailedException)
        {
            _logger.LogError(exception, "Could not load farm telemetry.");
            return ([], "Telemetry is unavailable. Check the Function App URL/key and the Function App's Table Storage permissions.");
        }
    }
}
