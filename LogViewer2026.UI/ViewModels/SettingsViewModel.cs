using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogViewer2026.Core.Configuration;
using LogViewer2026.Core.Services;
using System.IO;
using WpfMessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace LogViewer2026.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    [ObservableProperty]
    private string _pathFormat = "logs/log-.txt";

    [ObservableProperty]
    private string _rollingInterval = "Day";

    [ObservableProperty]
    private int _cacheSize = 10000;

    [ObservableProperty]
    private int _maxFileSizeMB = 2048;

    [ObservableProperty]
    private bool _enableIndexing = true;

    [ObservableProperty]
    private string _theme = "Light";

    [ObservableProperty]
    private int _maxRecentFiles = 10;

    [ObservableProperty]
    private bool _loadMultipleFiles = true;

    [ObservableProperty]
    private int _lookingGlassContextLines = 5;

    [ObservableProperty]
    private bool _autoUpdateLookingGlass = false;

    [ObservableProperty]
    private bool _filterSearchResults = false;

    [ObservableProperty]
    private bool _showLookingGlass = true;

    [ObservableProperty]
    private bool _reloadToLastRow = false;

    [ObservableProperty]
    private bool _useRegexSearch = false;

    [ObservableProperty]
    private string _settingsPath = string.Empty;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        SettingsPath = _settingsService.GetSettingsPath();
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();
        OutputTemplate = settings.OutputTemplate;
        PathFormat = settings.PathFormat;
        RollingInterval = settings.RollingInterval;
        CacheSize = settings.CacheSize;
        MaxFileSizeMB = settings.MaxFileSizeMB;
        EnableIndexing = settings.EnableIndexing;
        Theme = settings.Theme;
        MaxRecentFiles = settings.MaxRecentFiles;
        LoadMultipleFiles = settings.LoadMultipleFiles;
        LookingGlassContextLines = settings.LookingGlassContextLines;
        AutoUpdateLookingGlass = settings.AutoUpdateLookingGlass;
        FilterSearchResults = settings.FilterSearchResults;
        ShowLookingGlass = settings.ShowLookingGlass;
        ReloadToLastRow = settings.ReloadToLastRow;
        UseRegexSearch = settings.UseRegexSearch;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = new AppSettings
        {
            OutputTemplate = OutputTemplate,
            PathFormat = PathFormat,
            RollingInterval = RollingInterval,
            CacheSize = CacheSize,
            MaxFileSizeMB = MaxFileSizeMB,
            EnableIndexing = EnableIndexing,
            Theme = Theme,
            MaxRecentFiles = MaxRecentFiles,
            LoadMultipleFiles = LoadMultipleFiles,
            LookingGlassContextLines = LookingGlassContextLines,
            AutoUpdateLookingGlass = AutoUpdateLookingGlass,
            FilterSearchResults = FilterSearchResults,
            ShowLookingGlass = ShowLookingGlass,
            ReloadToLastRow = ReloadToLastRow,
            UseRegexSearch = UseRegexSearch
        };

        await _settingsService.SaveAsync(settings);

        // Apply theme immediately
        if (System.Windows.Application.Current is App app)
        {
            app.ApplyTheme(Theme);
        }

        WpfMessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var result = WpfMessageBox.Show(
            "Are you sure you want to reset all settings to defaults?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var settings = new AppSettings();
            await _settingsService.SaveAsync(settings);
            await LoadSettingsAsync();
            WpfMessageBox.Show("Settings reset to defaults!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        var result = WpfMessageBox.Show(
            "This will delete all cached log data. The cache will be rebuilt when you next open log files.\n\nAre you sure you want to clear the cache?",
            "Clear Cache",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LogViewer2026",
                    "Cache");

                if (Directory.Exists(cacheDir))
                {
                    // Delete all files in cache directory
                    var files = Directory.GetFiles(cacheDir);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore files that can't be deleted (e.g., locked database files)
                        }
                    }

                    await Task.Delay(100); // Small delay to ensure files are deleted

                    WpfMessageBox.Show(
                        $"Cache cleared successfully!\n\nDeleted {files.Length} cache file(s).",
                        "Cache Cleared",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    WpfMessageBox.Show(
                        "Cache directory does not exist or is already empty.",
                        "Cache Cleared",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"Error clearing cache: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
