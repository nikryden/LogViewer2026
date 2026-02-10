using LogViewer2026.Core.Interfaces;
using LogViewer2026.Core.Models;

namespace LogViewer2026.Infrastructure.Services;

public interface IMultiFileLogService : IDisposable
{
    Task<string> LoadFilesAsync(IEnumerable<string> filePaths);
    Task<string> LoadFolderAsync(string folderPath, string searchPattern = "*.log");
    IReadOnlyList<string> GetLoadedFiles();
}

public sealed class MultiFileLogService : IMultiFileLogService
{
    private readonly List<string> _loadedFiles = [];
    private string _combinedText = string.Empty;

    public async Task<string> LoadFilesAsync(IEnumerable<string> filePaths)
    {
        System.Diagnostics.Debug.WriteLine("MultiFileLogService: Loading multiple files...");
        
        _loadedFiles.Clear();
        var allText = new System.Text.StringBuilder();
        
        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
                continue;
                
            var text = await Task.Run(() =>
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            });
            allText.AppendLine($"=== File: {Path.GetFileName(filePath)} ===");
            allText.AppendLine(text);
            allText.AppendLine();
            
            _loadedFiles.Add(filePath);
        }
        
        _combinedText = allText.ToString();
        System.Diagnostics.Debug.WriteLine($"MultiFileLogService: Combined {_loadedFiles.Count} files");
        
        return _combinedText;
    }

    public async Task<string> LoadFolderAsync(string folderPath, string searchPattern = "*.log")
    {
        System.Diagnostics.Debug.WriteLine($"MultiFileLogService: Loading folder '{folderPath}' with pattern '{searchPattern}'...");
        
        var files = Directory.GetFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();
            
        return await LoadFilesAsync(files);
    }

    public IReadOnlyList<string> GetLoadedFiles() => _loadedFiles.AsReadOnly();

    public void Dispose()
    {
        _loadedFiles.Clear();
        _combinedText = string.Empty;
    }
}
