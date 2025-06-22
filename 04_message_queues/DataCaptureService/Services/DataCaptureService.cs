
using Azure.Messaging.ServiceBus;
using DataCaptureService.Models;
using System.Text.Json;

namespace DataCaptureService.Services;

public class DataCaptureService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _chunkSender;
    private readonly ServiceBusReceiver _resultReceiver;
    private readonly FileWatcher _fileWatcher;
    private readonly FileChunkingService _chunkingService;
    private readonly string _serviceId;
    private readonly string _inputPath;
    private readonly string[] _supportedExtensions;
    private bool _isRunning;

    public DataCaptureService(string connectionString, string inputPath, string serviceId,
                             string[] supportedExtensions, int chunkSize)
    {
        _inputPath = inputPath;
        _serviceId = serviceId;
        _supportedExtensions = supportedExtensions ?? new[] { ".pdf", ".mp4", ".zip", ".docx" };

        _serviceBusClient = new ServiceBusClient(connectionString);
        _chunkSender = _serviceBusClient.CreateSender("document-chunks");
        _resultReceiver = _serviceBusClient.CreateReceiver("processing-results");

        _chunkingService = new FileChunkingService(chunkSize);
        _fileWatcher = new FileWatcher(_inputPath, _supportedExtensions, OnFileDetected);

        Console.WriteLine($"Data Capture Service [{_serviceId}] initialized");
        Console.WriteLine($"Monitoring: {Path.GetFullPath(_inputPath)}");
        Console.WriteLine($"Supported formats: {string.Join(", ", _supportedExtensions)}");
        Console.WriteLine($"Chunk size: {chunkSize / 1024}KB");
    }

    public async Task StartAsync()
    {
        _isRunning = true;
        Console.WriteLine($"\n Data Capture Service [{_serviceId}] started");
        Console.WriteLine("Waiting for files... (Press Ctrl+C to stop)\n");

        // Start listening for processing results
        _ = Task.Run(ListenForResultsAsync);

        // Process any existing files
        await ProcessExistingFilesAsync();

        // Keep running
        while (_isRunning)
        {
            await Task.Delay(1000);
        }
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        _fileWatcher?.Stop();
        await _chunkSender.CloseAsync();
        await _resultReceiver.CloseAsync();
        await _serviceBusClient.DisposeAsync();
        Console.WriteLine("Data Capture Service stopped.");
    }

    private async Task ProcessExistingFilesAsync()
    {
        if (!Directory.Exists(_inputPath))
        {
            Directory.CreateDirectory(_inputPath);
            return;
        }

        var files = Directory.GetFiles(_inputPath)
            .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToArray();

        if (files.Length > 0)
        {
            Console.WriteLine($"Found {files.Length} existing file(s) to process:");
            foreach (var file in files)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            Console.WriteLine();

            foreach (var file in files)
            {
                await ProcessFileAsync(file);
            }
        }
    }

    private async void OnFileDetected(string filePath)
    {
        await ProcessFileAsync(filePath);
    }

    private async Task ProcessFileAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            Console.WriteLine($"Processing: {fileInfo.Name} ({fileInfo.Length:N0} bytes)");

            var chunks = await _chunkingService.ChunkFileAsync(filePath, _serviceId);

            Console.WriteLine($"Sending {chunks.Count} chunks to queue...");

            foreach (var chunk in chunks)
            {
                var message = new ServiceBusMessage(JsonSerializer.Serialize(chunk))
                {
                    SessionId = chunk.SessionId,
                    MessageId = $"{chunk.SessionId}-{chunk.ChunkIndex}",
                    TimeToLive = TimeSpan.FromHours(24)
                };

                await _chunkSender.SendMessageAsync(message);

                if (chunk.ChunkIndex % 50 == 0 || chunk.ChunkIndex == chunk.TotalChunks - 1)
                {
                    var progress = (double)(chunk.ChunkIndex + 1) / chunk.TotalChunks * 100;
                    Console.WriteLine($"  Sent {chunk.ChunkIndex + 1}/{chunk.TotalChunks} chunks ({progress:F1}%)");
                }
            }

            // Move processed file to avoid reprocessing
            var processedPath = Path.Combine(_inputPath, "processed");
            if (!Directory.Exists(processedPath))
                Directory.CreateDirectory(processedPath);

            var newPath = Path.Combine(processedPath, fileInfo.Name);
            if (File.Exists(newPath))
                File.Delete(newPath);

            File.Move(filePath, newPath);

            Console.WriteLine($"File processing completed: {fileInfo.Name}");
            Console.WriteLine($"Moved to: processed/{fileInfo.Name}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {filePath}: {ex.Message}\n");
        }
    }

    private async Task ListenForResultsAsync()
    {
        while (_isRunning)
        {
            try
            {
                var message = await _resultReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
                if (message != null)
                {
                    var result = JsonSerializer.Deserialize<ProcessingResultMessage>(message.Body.ToString());
                    Console.WriteLine($"Processing Result: {result.FileName} - {result.Status}");
                    if (!string.IsNullOrEmpty(result.Message))
                        Console.WriteLine($"   Message: {result.Message}");

                    await _resultReceiver.CompleteMessageAsync(message);
                }
            }
            catch (Exception ex) when (ex.Message.Contains("timeout"))
            {
                // Timeout is normal, continue listening
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving result: {ex.Message}");
                await Task.Delay(5000);
            }
        }
    }
}
