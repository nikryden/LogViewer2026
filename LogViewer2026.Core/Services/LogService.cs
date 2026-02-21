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

        // Read with FileShare to allow opening files that are being written to by other processes
        var text = await Task.Run(() =>
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });

        // Report line count
        var lineCount = GetTotalLineCount(text);
        progress?.Report(lineCount);

        System.Diagnostics.Debug.WriteLine($"LogService: Read {text.Length:N0} characters, {lineCount:N0} lines");

        return text;
    }

    public int GetTotalLineCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int count = 1;
        foreach (var c in text.AsSpan())
        {
            if (c == '\n')
                count++;
        }
        return count;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
