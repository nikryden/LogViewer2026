using LogViewer2026.Core.Models;

namespace LogViewer2026.Core.Configuration;

public sealed class FilterConfiguration
{
    public string Name { get; set; } = "Default Filter";
    public LogLevel? LogLevel { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? SearchText { get; set; }
    public string? SourceContextFilter { get; set; }
    public bool ExcludeVerbose { get; set; }
    public bool ExcludeDebug { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FilterConfigurationCollection
{
    public List<FilterConfiguration> Filters { get; set; } = [];
    public string? LastUsedFilter { get; set; }
}
