namespace LogViewer2026.Core.Models;

public sealed class LogEntry
{
    public required DateTime Timestamp { get; init; }
    public required LogLevel Level { get; init; }
    public required string Message { get; init; }
    public string? Exception { get; init; }
    public Dictionary<string, object>? Properties { get; init; }
    public long FileOffset { get; init; }
    public int LineNumber { get; init; }
    public string? SourceContext { get; init; }
}
