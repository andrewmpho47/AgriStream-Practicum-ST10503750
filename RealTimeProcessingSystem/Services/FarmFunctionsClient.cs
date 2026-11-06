using System.Net;
using System.Net.Http.Json;
using RealTimeProcessingSystem.Models;

namespace RealTimeProcessingSystem.Services;

public sealed class FarmFunctionsClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public FarmFunctionsClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<SensorReadingViewModel>> GetSensorReadingsAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, "telemetry");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Telemetry Function returned {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<List<SensorReadingViewModel>>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("Telemetry Function returned an empty or invalid response.");
    }

    public async Task RequestIrrigationAsync(string deviceId, string command, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "irrigation");
        request.Content = JsonContent.Create(new { deviceId, command });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            throw new HttpRequestException(
                $"Irrigation Function returned {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string route)
    {
        var baseUrl = _configuration["FarmFunctions:BaseUrl"]
            ?? throw new InvalidOperationException("FarmFunctions__BaseUrl must be configured.");
        var key = _configuration["FarmFunctions:Key"]
            ?? throw new InvalidOperationException("FarmFunctions__Key must be configured in server-side application settings.");
        var baseUri = new Uri($"{baseUrl.TrimEnd('/')}/api/{route}");
        var request = new HttpRequestMessage(method, baseUri);
        request.Headers.Add("x-functions-key", key);
        return request;
    }
}
