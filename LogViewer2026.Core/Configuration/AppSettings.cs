namespace LogViewer2026.Core.Configuration;

public sealed class AppSettings
{
    public string OutputTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    public string PathFormat { get; set; } = "logs/log-.txt";
    public string RollingInterval { get; set; } = "Day";
    public int MaxFileSizeMB { get; set; } = 2048;
    public string Theme { get; set; } = "Light";
    public List<string> RecentFiles { get; set; } = [];
    public int MaxRecentFiles { get; set; } = 10;
    public bool LoadMultipleFiles { get; set; } = true;
    public string LastOpenedFolder { get; set; } = string.Empty;
    public int LookingGlassContextLines { get; set; } = 5;
    public bool AutoUpdateLookingGlass { get; set; } = false;
    public bool FilterSearchResults { get; set; } = false;
    public bool ShowLookingGlass { get; set; } = true;
    public bool ReloadToLastRow { get; set; } = false;
    public bool UseRegexSearch { get; set; } = false;
    public bool UseMultilineRegex { get; set; } = false;
    public bool AutoReloadOnChange { get; set; } = false;
    // Font sizes for the editors
    public double LogEditorFontSize { get; set; } = 12.0;
    public double LookingGlassFontSize { get; set; } = 10.0;
    // How much to change font size per mouse wheel step when Ctrl is held
    public double FontSizeWheelStep { get; set; } = 1.0;
}
