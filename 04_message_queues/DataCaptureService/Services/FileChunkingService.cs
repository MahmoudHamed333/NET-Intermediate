using DataCaptureService.Models;
using System.Security.Cryptography;

namespace DataCaptureService.Services;

public class FileChunkingService
{
    private readonly int _chunkSize;

    public FileChunkingService(int chunkSize = 900 * 1024)
    {
        _chunkSize = chunkSize;
    }

    public async Task<List<FileChunkMessage>> ChunkFileAsync(string filePath, string sourceService)
    {
        var fileInfo = new FileInfo(filePath);
        var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / _chunkSize);
        var sessionId = Guid.NewGuid().ToString();
        var chunks = new List<FileChunkMessage>();

        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            var buffer = new byte[_chunkSize];
            var chunkIndex = 0;

            while (fileStream.Position < fileStream.Length)
            {
                var bytesRead = await fileStream.ReadAsync(buffer, 0, _chunkSize);
                var chunkData = new byte[bytesRead];
                Array.Copy(buffer, chunkData, bytesRead);

                chunks.Add(new FileChunkMessage
                {
                    SessionId = sessionId,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    ChunkIndex = chunkIndex,
                    TotalChunks = totalChunks,
                    ChunkData = Convert.ToBase64String(chunkData),
                    Checksum = CalculateChecksum(chunkData),
                    Timestamp = DateTime.UtcNow,
                    SourceService = sourceService
                });

                chunkIndex++;
            }
        }

        return chunks;
    }

    public string CalculateChecksum(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}