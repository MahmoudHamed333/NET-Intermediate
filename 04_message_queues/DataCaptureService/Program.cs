using Microsoft.Extensions.Configuration;

Console.WriteLine("=== Data Capture Service Starting ===");

var config = new ConfigurationBuilder()
   .AddJsonFile("appsettings.json")
   .Build();

var serviceBusConfig = config.GetSection("ServiceBus");
var dataCaptureConfig = config.GetSection("DataCapture");

var service = new DataCaptureService.Services.DataCaptureService(
   serviceBusConfig["ConnectionString"],
   dataCaptureConfig["InputPath"],
   dataCaptureConfig["ServiceId"],
   dataCaptureConfig.GetSection("SupportedExtensions").Get<string[]>(),
   int.Parse(dataCaptureConfig["ChunkSizeKB"]) * 1024
);
Console.WriteLine($"Scanning directory: {Path.GetFullPath(dataCaptureConfig["InputPath"])}");

Console.CancelKeyPress += async (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down Data Capture Service...");
    await service.StopAsync();
};

await service.StartAsync();
