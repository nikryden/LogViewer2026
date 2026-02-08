using LogViewer2026.Core.Interfaces;
using LogViewer2026.Core.Models;

namespace LogViewer2026.Core.Services;

public interface ILogService : IDisposable
{
    Task<string> LoadFileAsync(string filePath, IProgress<int>? progress = null);
    int GetTotalLineCount(string text);
}

public sealed class LogService : ILogService
{
    public async Task<string> LoadFileAsync(string filePath, IProgress<int>? progress = null)
    {
        System.Diagnostics.Debug.WriteLine($"LogService: Reading entire file '{filePath}'...");

        // Simple! Just read all text
        var text = await Task.Run(() => File.ReadAllText(filePath));

        // Report line count
        var lineCount = text.Split('\n').Length;
        progress?.Report(lineCount);

        System.Diagnostics.Debug.WriteLine($"LogService: Read {text.Length:N0} characters, {lineCount:N0} lines");

        return text;
    }

    public int GetTotalLineCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return text.Split('\n').Length;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
