using LogViewer2026.Core.Models;

namespace LogViewer2026.UI.Models;

public sealed class LogLevelOption
{
    public string DisplayName { get; init; } = string.Empty;
    public LogLevel? Value { get; init; }

    public override string ToString() => DisplayName;
}
