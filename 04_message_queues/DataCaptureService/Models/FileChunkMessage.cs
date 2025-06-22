namespace DataCaptureService.Models;

public class FileChunkMessage
{
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public string ChunkData { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SourceService { get; set; } = string.Empty;
}