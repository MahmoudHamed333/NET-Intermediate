namespace ProcessingService.Models;
public class FileTransferSession
{
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public HashSet<int> ReceivedChunks { get; set; } = new HashSet<int>();
    public Dictionary<int, byte[]> ChunkData { get; set; } = new Dictionary<int, byte[]>();
    public DateTime StartTime { get; set; }
    public DateTime LastActivity { get; set; }
    public string SourceService { get; set; } = string.Empty;

    public bool IsComplete => ReceivedChunks.Count == TotalChunks && TotalChunks > 0;
    public double Progress => TotalChunks > 0 ? (double)ReceivedChunks.Count / TotalChunks * 100 : 0;
}