using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using sl_img_prcr.Services;


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        //My services
        services.AddSingleton<BlobStorageService>();
        services.AddSingleton<ImageProcessorService>();
        services.AddSingleton<RandomTitleService>();
        // Register blob service with null check
        services.AddSingleton(x => {
            string? blobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            
            if (string.IsNullOrEmpty(blobConnectionString))
            {
                throw new InvalidOperationException("The Azure Storage connection string is missing.");
            }

            return new BlobServiceClient(blobConnectionString);
        });
    })
    .Build();

host.Run();
