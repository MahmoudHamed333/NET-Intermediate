
namespace DataCaptureService.Services;

public class FileWatcher
{
    private readonly FileSystemWatcher _watcher;
    private readonly string[] _supportedExtensions;
    private readonly Action<string> _onFileDetected;

    public FileWatcher(string watchPath, string[] supportedExtensions, Action<string> onFileDetected)
    {
        _supportedExtensions = supportedExtensions;
        _onFileDetected = onFileDetected;

        if (!Directory.Exists(watchPath))
            Directory.CreateDirectory(watchPath);

        _watcher = new FileSystemWatcher(watchPath)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
        };

        _watcher.Created += OnFileCreated;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var extension = Path.GetExtension(e.FullPath).ToLower();
        if (_supportedExtensions.Contains(extension))
        {
            // Wait for file to be completely written
            Thread.Sleep(2000);
            if (IsFileReady(e.FullPath))
            {
                _onFileDetected(e.FullPath);
            }
        }
    }

    private bool IsFileReady(string filePath)
    {
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                return true;
        }
        catch
        {
            return false;
        }
    }

    public void Stop() => _watcher?.Dispose();
}