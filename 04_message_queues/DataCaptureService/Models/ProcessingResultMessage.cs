namespace DataCaptureService.Models;

public class ProcessingResultMessage
{
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public long ProcessedFileSize { get; set; }
    public string OutputPath { get; set; } = string.Empty;
}