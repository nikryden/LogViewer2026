namespace LogViewer2026.Core.Interfaces;

public interface IFileReader : IDisposable
{
    void Open(string filePath);
    Task OpenAsync(string filePath, IProgress<int>? progress = null); // Progressive loading
    string ReadLine(long offset);
    string ReadLineByIndex(int lineIndex); // Fast path: direct index access
    IEnumerable<(long offset, string line)> ReadLines(long startOffset, int count);
    long GetLineOffset(int lineNumber);
    int GetTotalLines();

    // Parallel reading support for maximum performance
    bool SupportsParallelReading { get; }
}
