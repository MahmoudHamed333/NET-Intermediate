using ProcessingService.Models;

namespace ProcessingService.Services;

public class FileAssemblyService
{
    public async Task<string> AssembleFileAsync(FileTransferSession session, string outputPath)
    {
        var tempFilePath = Path.Combine(outputPath, $"{session.SessionId}_{session.FileName}.tmp");

        using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
        {
            for (int i = 0; i < session.TotalChunks; i++)
            {
                if (!session.ChunkData.TryGetValue(i, out var chunk))
                {
                    throw new InvalidOperationException($"Missing chunk {i} for file {session.FileName}");
                }

                await fileStream.WriteAsync(chunk, 0, chunk.Length);
            }
        }

        var finalFilePath = Path.Combine(outputPath, session.FileName);
        File.Move(tempFilePath, finalFilePath);

        return finalFilePath;
    }
}
