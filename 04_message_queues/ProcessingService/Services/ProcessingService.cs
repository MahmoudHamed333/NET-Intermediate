using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ProcessingService.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace ProcessingService.Services;

public class ProcessingService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly ServiceBusReceiver _chunkReceiver;
    private readonly ServiceBusSender _resultSender;
    private readonly FileAssemblyService _assemblyService;
    private readonly string _outputPath;
    private readonly string _tempPath;
    private readonly ConcurrentDictionary<string, FileTransferSession> _activeSessions;
    private bool _isRunning;

    public ProcessingService(string connectionString, string outputPath, string tempPath)
    {
        _outputPath = outputPath;
        _tempPath = tempPath;
        _activeSessions = new ConcurrentDictionary<string, FileTransferSession>();

        _serviceBusClient = new ServiceBusClient(connectionString);
        _adminClient = new ServiceBusAdministrationClient(connectionString);
        _chunkReceiver = _serviceBusClient.CreateReceiver("document-chunks");
        _resultSender = _serviceBusClient.CreateSender("processing-results");

        _assemblyService = new FileAssemblyService();

        // Ensure directories exist
        Directory.CreateDirectory(_outputPath);
        Directory.CreateDirectory(_tempPath);

        Console.WriteLine("Processing Service initialized");
        Console.WriteLine($"Output path: {Path.GetFullPath(_outputPath)}");
        Console.WriteLine($"Temp path: {Path.GetFullPath(_tempPath)}");
    }

    public async Task StartAsync()
    {
        _isRunning = true;

        // Setup queues
        await SetupQueuesAsync();

        Console.WriteLine("\nProcessing Service started");
        Console.WriteLine("Listening for file chunks... (Press Ctrl+C to stop)\n");

        // Start processing chunks
        _ = Task.Run(ProcessChunksAsync);

        // Start cleanup task
        _ = Task.Run(CleanupStaleSessionsAsync);

        // Keep running
        while (_isRunning)
        {
            await Task.Delay(1000);
        }
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        await _chunkReceiver.CloseAsync();
        await _resultSender.CloseAsync();
        await _serviceBusClient.DisposeAsync();
        Console.WriteLine("Processing Service stopped.");
    }

    private async Task SetupQueuesAsync()
    {
        try
        {
            // Create document chunk queue if it doesn't exist
            if (!await _adminClient.QueueExistsAsync("document-chunks"))
            {
                var chunkQueueOptions = new CreateQueueOptions("document-chunks")
                {
                    MaxSizeInMegabytes = 5120, // 5GB
                    DefaultMessageTimeToLive = TimeSpan.FromHours(24),
                    EnableBatchedOperations = true
                };
                await _adminClient.CreateQueueAsync(chunkQueueOptions);
                Console.WriteLine("Created document-chunks queue");
            }

            // Create processing results queue if it doesn't exist
            if (!await _adminClient.QueueExistsAsync("processing-results"))
            {
                var resultQueueOptions = new CreateQueueOptions("processing-results")
                {
                    MaxSizeInMegabytes = 1024, // 1GB
                    DefaultMessageTimeToLive = TimeSpan.FromHours(24)
                };
                await _adminClient.CreateQueueAsync(resultQueueOptions);
                Console.WriteLine("Created processing-results queue");
            }

            Console.WriteLine("Queue setup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting up queues: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessChunksAsync()
    {
        while (_isRunning)
        {
            try
            {
                var message = await _chunkReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));
                if (message != null)
                {
                    await ProcessChunkMessageAsync(message);
                }
            }
            catch (Exception ex) when (ex.Message.Contains("timeout"))
            {
                // Timeout is normal, continue processing
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing chunk: {ex.Message}");
                await Task.Delay(5000);
            }
        }
    }

    private async Task ProcessChunkMessageAsync(ServiceBusReceivedMessage message)
    {
        try
        {
            var chunk = JsonSerializer.Deserialize<FileChunkMessage>(message.Body.ToString());

            // Get or create session
            var session = _activeSessions.GetOrAdd(chunk.SessionId, _ => new FileTransferSession
            {
                SessionId = chunk.SessionId,
                FileName = chunk.FileName,
                FileSize = chunk.FileSize,
                TotalChunks = chunk.TotalChunks,
                StartTime = DateTime.UtcNow,
                SourceService = chunk.SourceService
            });

            session.LastActivity = DateTime.UtcNow;

            // Decode and store chunk data
            var chunkData = Convert.FromBase64String(chunk.ChunkData);

            // Verify chunk integrity
            var calculatedChecksum = CalculateChecksum(chunkData);
            if (calculatedChecksum != chunk.Checksum)
            {
                Console.WriteLine($"Checksum mismatch for chunk {chunk.ChunkIndex} of {chunk.FileName}");
                return;
            }

            // Store chunk
            session.ChunkData[chunk.ChunkIndex] = chunkData;
            session.ReceivedChunks.Add(chunk.ChunkIndex);

            // Progress update
            if (session.ReceivedChunks.Count % 50 == 0 || session.IsComplete)
            {
                Console.WriteLine($" {chunk.FileName}: {session.ReceivedChunks.Count}/{session.TotalChunks} chunks ({session.Progress:F1}%)");
            }

            // Complete message
            await _chunkReceiver.CompleteMessageAsync(message);

            // Check if file is complete
            if (session.IsComplete)
            {
                await CompleteFileAsync(session);
            }

            // Send progress update
            await SendResultAsync(new ProcessingResultMessage
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                Status = session.IsComplete ? "Completed" : "InProgress",
                Message = $"Progress: {session.Progress:F1}%",
                ProcessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing chunk message: {ex.Message}");
        }
    }

    private async Task CompleteFileAsync(FileTransferSession session)
    {
        try
        {
            Console.WriteLine($"Assembling file: {session.FileName}");

            var outputPath = await _assemblyService.AssembleFileAsync(session, _outputPath);

            // Verify file size
            var assembledFileInfo = new FileInfo(outputPath);
            if (assembledFileInfo.Length != session.FileSize)
            {
                throw new InvalidOperationException($"File size mismatch. Expected: {session.FileSize}, Actual: {assembledFileInfo.Length}");
            }

            // Remove from active sessions
            _activeSessions.TryRemove(session.SessionId, out _);

            Console.WriteLine($"   File completed: {session.FileName}");
            Console.WriteLine($"   Size: {assembledFileInfo.Length:N0} bytes");
            Console.WriteLine($"   Saved to: {outputPath}");
            Console.WriteLine($"   Processing time: {DateTime.UtcNow - session.StartTime:mm\\:ss}\n");

            // Send completion result
            await SendResultAsync(new ProcessingResultMessage
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                Status = "Completed",
                Message = $"File successfully processed and saved",
                ProcessedAt = DateTime.UtcNow,
                ProcessedFileSize = assembledFileInfo.Length,
                OutputPath = outputPath
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error completing file {session.FileName}: {ex.Message}");

            await SendResultAsync(new ProcessingResultMessage
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                Status = "Failed",
                Message = ex.Message,
                ProcessedAt = DateTime.UtcNow
            });
        }
    }

    private async Task SendResultAsync(ProcessingResultMessage result)
    {
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(result))
            {
                MessageId = Guid.NewGuid().ToString(),
                TimeToLive = TimeSpan.FromHours(24)
            };

            await _resultSender.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not send result message: {ex.Message}");
        }
    }

    private async Task CleanupStaleSessionsAsync()
    {
        var staleTimeout = TimeSpan.FromHours(2);

        while (_isRunning)
        {
            try
            {
                var now = DateTime.UtcNow;
                var staleSessions = _activeSessions.Values
                    .Where(s => now - s.LastActivity > staleTimeout)
                    .ToList();

                foreach (var session in staleSessions)
                {
                    Console.WriteLine($"Cleaning up stale session: {session.FileName}");

                    // Remove from active sessions
                    _activeSessions.TryRemove(session.SessionId, out _);

                    // Optionally, clean up any temp files or resources here

                    // Send a failed result message
                    await SendResultAsync(new ProcessingResultMessage
                    {
                        SessionId = session.SessionId,
                        FileName = session.FileName,
                        Status = "Failed",
                        Message = "Session timed out due to inactivity.",
                        ProcessedAt = DateTime.UtcNow
                    });
                }

                if (_activeSessions.Count > 0)
                {
                    Console.WriteLine($"Active sessions: {_activeSessions.Count}");
                }

                // Run cleanup every 10 minutes
                await Task.Delay(TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error during stale session cleanup: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }

    private string CalculateChecksum(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}