namespace LogViewer2026.Core.Configuration;

public sealed class AppSettings
{
    public string OutputTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    public string PathFormat { get; set; } = "logs/log-.txt";
    public string RollingInterval { get; set; } = "Day";
    public int CacheSize { get; set; } = 10000;
    public int MaxFileSizeMB { get; set; } = 2048;
    public bool EnableIndexing { get; set; } = true;
    public string Theme { get; set; } = "Light";
    public List<string> RecentFiles { get; set; } = [];
    public int MaxRecentFiles { get; set; } = 10;
    public bool LoadMultipleFiles { get; set; } = true;
    public string LastOpenedFolder { get; set; } = string.Empty;
    public int LookingGlassContextLines { get; set; } = 5;
    public bool AutoUpdateLookingGlass { get; set; } = false;
    public bool FilterSearchResults { get; set; } = false;
    public bool ShowLookingGlass { get; set; } = true;
}
