
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== Main Processing Service Starting ===");

var config = new ConfigurationBuilder()
   .AddJsonFile("appsettings.json")
   .Build();

var serviceBusConfig = config.GetSection("ServiceBus");
var processingConfig = config.GetSection("Processing");

var service = new ProcessingService.Services.ProcessingService(
   serviceBusConfig["ConnectionString"],
   processingConfig["OutputPath"],
   processingConfig["TempPath"]
);

Console.CancelKeyPress += async (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down Processing Service...");
    await service.StopAsync();
};

await service.StartAsync();