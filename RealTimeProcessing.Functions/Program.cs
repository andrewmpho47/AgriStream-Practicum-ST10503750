using Azure.Core;
using Azure.Identity;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealTimeProcessing.Functions.Data;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();
var storageConnection = configuration["FarmStorage"];

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<TokenCredential>(_ =>
            string.Equals(configuration["AZURE_FUNCTIONS_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)
                ? new DefaultAzureCredential()
                : new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()));
        if (!string.IsNullOrWhiteSpace(storageConnection))
        {
            services.AddSingleton(_ => new TableServiceClient(storageConnection));
            services.AddSingleton(_ => new BlobServiceClient(storageConnection));
            services.AddSingleton(_ => new QueueServiceClient(storageConnection));
        }
        else
        {
            services.AddSingleton(sp => new TableServiceClient(
                new Uri(Required(configuration, "FarmStorage:tableServiceUri")),
                sp.GetRequiredService<TokenCredential>()));
            services.AddSingleton(sp => new BlobServiceClient(
                new Uri(Required(configuration, "FarmStorage:blobServiceUri")),
                sp.GetRequiredService<TokenCredential>()));
            services.AddSingleton(sp => new QueueServiceClient(
                new Uri(Required(configuration, "FarmStorage:queueServiceUri")),
                sp.GetRequiredService<TokenCredential>()));
        }
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("SensorReadings"));
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("BlobProcessingLog"));
        services.AddSingleton<SqlConnectionFactory>();
        services.AddSingleton<FarmSqlRepository>();
    })
    .Build();

await host.RunAsync();

static string Required(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"The {key} application setting is required.");
