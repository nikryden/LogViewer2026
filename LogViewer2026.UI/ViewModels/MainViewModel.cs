using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogViewer2026.Core.Configuration;
using LogViewer2026.Core.Models;
using LogViewer2026.Core.Services;
using LogViewer2026.Infrastructure.Services;
using LogViewer2026.UI.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using WpfApp = System.Windows.Application;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace LogViewer2026.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ILogService _logService;
    private readonly IMultiFileLogService? _multiFileLogService;
    private readonly IFilterConfigurationService _filterConfigService;
    private readonly ISettingsService _settingsService;
    private bool _isMultiFileMode;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = [];

    [ObservableProperty]
    private string _originalLogText = string.Empty; // Store original unfiltered text

    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private LogEntry? _selectedLogEntry;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _currentSearchResultIndex = -1;

    [ObservableProperty]
    private int _totalSearchResults = 0;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _totalLogCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _progressPercentage;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private LogLevel? _filterLevel;

    [ObservableProperty]
    private DateTime? _filterStartTime;

    [ObservableProperty]
    private DateTime? _filterEndTime;

    [ObservableProperty]
    private ObservableCollection<FilterConfiguration> _savedFilters = [];

    [ObservableProperty]
    private FilterConfiguration? _selectedFilter;

    [ObservableProperty]
    private string _loadedFilesInfo = string.Empty;

    [ObservableProperty]
    private LogLevelOption? _selectedLogLevelOption;

    public IEnumerable<LogLevelOption> AvailableLogLevels { get; } = new[]
    {
        new LogLevelOption { DisplayName = "All", Value = null },
        new LogLevelOption { DisplayName = "Verbose", Value = LogLevel.Verbose },
        new LogLevelOption { DisplayName = "Debug", Value = LogLevel.Debug },
        new LogLevelOption { DisplayName = "Information", Value = LogLevel.Information },
        new LogLevelOption { DisplayName = "Warning", Value = LogLevel.Warning },
        new LogLevelOption { DisplayName = "Error", Value = LogLevel.Error },
        new LogLevelOption { DisplayName = "Fatal", Value = LogLevel.Fatal }
    };

    partial void OnSelectedLogLevelOptionChanged(LogLevelOption? value)
    {
        FilterLevel = value?.Value;
    }

    public MainViewModel(
        ILogService logService, 
        IMultiFileLogService multiFileLogService,
        IFilterConfigurationService filterConfigService,
        ISettingsService settingsService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _multiFileLogService = multiFileLogService;
        _filterConfigService = filterConfigService ?? throw new ArgumentNullException(nameof(filterConfigService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Initialize to "All"
        SelectedLogLevelOption = AvailableLogLevels.First();

        _ = LoadSavedFiltersAsync();
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Log Files (*.log;*.txt)|*.log;*.txt|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Open Log File"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadFileAsync(dialog.FileName);
        }
    }

    private async Task LoadFileAsync(string filePath)
    {
        IsLoading = true;
        StatusText = $"Loading {Path.GetFileName(filePath)}...";
        _isMultiFileMode = false;

        try
        {
            // Simple! Just read entire file
            var progress = new Progress<int>(lineCount =>
            {
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    StatusText = $"Loading {Path.GetFileName(filePath)}... {lineCount:N0} lines";
                });
            });

            var fileText = await _logService.LoadFileAsync(filePath, progress);
            TotalLogCount = _logService.GetTotalLineCount(fileText);

            // Store original text and display it
            OriginalLogText = fileText;
            LogText = fileText;

            StatusText = $"Loaded {Path.GetFileName(filePath)} - {TotalLogCount:N0} lines ({fileText.Length:N0} characters)";
            LoadedFilesInfo = $"File: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading file: {ex.Message}";
            WpfMessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadLogEntriesAsync(int startIndex, int count)
    {
        // Not needed anymore - text is loaded all at once!
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        // Simple - use AvalonEdit's built-in search (Ctrl+F)
        StatusText = "Use Ctrl+F to search in the text editor";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void FindNext()
    {
        if (TotalSearchResults == 0)
            return;

        CurrentSearchResultIndex = (CurrentSearchResultIndex + 1) % TotalSearchResults;
        StatusText = $"Result {CurrentSearchResultIndex + 1} of {TotalSearchResults}";

        // Notify UI to scroll to current result
        OnSearchResultChanged?.Invoke(CurrentSearchResultIndex);
    }

    [RelayCommand]
    private void FindPrevious()
    {
        if (TotalSearchResults == 0)
            return;

        CurrentSearchResultIndex = CurrentSearchResultIndex <= 0 
            ? TotalSearchResults - 1 
            : CurrentSearchResultIndex - 1;

        StatusText = $"Result {CurrentSearchResultIndex + 1} of {TotalSearchResults}";

        // Notify UI to scroll to current result
        OnSearchResultChanged?.Invoke(CurrentSearchResultIndex);
    }

    public event Action<int>? OnSearchResultChanged;

    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        if (string.IsNullOrEmpty(OriginalLogText))
        {
            StatusText = "No log file loaded";
            return;
        }

        IsLoading = true;
        StatusText = "Applying filter...";

        try
        {
            await Task.Run(() =>
            {
                var lines = OriginalLogText.Split('\n');
                var filteredLines = new List<string>();

                foreach (var line in lines)
                {
                    // If no level filter, include all lines
                    if (FilterLevel == null)
                    {
                        filteredLines.Add(line);
                        continue;
                    }

                    // Check if line contains the log level
                    if (ContainsLogLevel(line, FilterLevel.Value))
                    {
                        filteredLines.Add(line);
                    }
                }

                var filteredText = string.Join("\n", filteredLines);
                var filteredCount = filteredLines.Count;

                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    LogText = filteredText;

                    if (FilterLevel == null)
                    {
                        StatusText = $"Showing all {filteredCount:N0} lines";
                    }
                    else
                    {
                        StatusText = $"Filtered to {filteredCount:N0} lines with level: {FilterLevel}";
                    }
                });
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Filter error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool ContainsLogLevel(string line, LogLevel level)
    {
        // Common log level patterns
        var levelStr = level.ToString();

        // Check various formats:
        // [ERROR], [Error], ERROR, Error, [ERR], ERR
        var patterns = new[]
        {
            $"[{levelStr}]",
            $"[{levelStr.ToUpper()}]",
            $"[{levelStr.ToLower()}]",
            $" {levelStr} ",
            $" {levelStr.ToUpper()} ",
            $" {levelStr.ToLower()} ",
            $":{levelStr}:",
            $":{levelStr.ToUpper()}:",
            $":{levelStr.ToLower()}:",
        };

        // Special cases for common abbreviations
        if (level == LogLevel.Information)
        {
            patterns = patterns.Concat(new[] { "[INF]", "[INFO]", " INF ", " INFO " }).ToArray();
        }
        else if (level == LogLevel.Warning)
        {
            patterns = patterns.Concat(new[] { "[WRN]", "[WARN]", " WRN ", " WARN " }).ToArray();
        }
        else if (level == LogLevel.Error)
        {
            patterns = patterns.Concat(new[] { "[ERR]", " ERR " }).ToArray();
        }
        else if (level == LogLevel.Debug)
        {
            patterns = patterns.Concat(new[] { "[DBG]", " DBG " }).ToArray();
        }
        else if (level == LogLevel.Verbose)
        {
            patterns = patterns.Concat(new[] { "[VRB]", " VRB ", "[TRACE]", " TRACE " }).ToArray();
        }
        else if (level == LogLevel.Fatal)
        {
            patterns = patterns.Concat(new[] { "[FTL]", " FTL ", "[CRITICAL]", " CRITICAL " }).ToArray();
        }

        return patterns.Any(p => line.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedLogLevelOption = AvailableLogLevels.First(); // Reset to "All"
        FilterLevel = null;
        FilterStartTime = null;
        FilterEndTime = null;
        SearchText = string.Empty;

        // Restore original text
        if (!string.IsNullOrEmpty(OriginalLogText))
        {
            LogText = OriginalLogText;
            StatusText = $"Filters cleared - showing all {TotalLogCount:N0} lines";
        }
        else
        {
            StatusText = "Filters cleared";
        }
    }

    [ObservableProperty]
    private string _selectedText = string.Empty;

    [ObservableProperty]
    private string _currentLine = string.Empty;

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(SelectedText))
        {
            WpfClipboard.SetText(SelectedText);
            StatusText = "Copied selected text to clipboard";
        }
        else
        {
            StatusText = "No text selected";
        }
    }

    [RelayCommand]
    private void CopyToSearch()
    {
        if (!string.IsNullOrEmpty(SelectedText))
        {
            SearchText = SelectedText;
            StatusText = $"Copied to search: '{SelectedText}'";
        }
        else
        {
            StatusText = "No text selected";
        }
    }

    [RelayCommand]
    private void CopyWholeLine()
    {
        if (!string.IsNullOrEmpty(CurrentLine))
        {
            WpfClipboard.SetText(CurrentLine);
            StatusText = "Copied entire line to clipboard";
        }
        else
        {
            StatusText = "No line to copy";
        }
    }

    [RelayCommand]
    private void SelectWholeLine()
    {
        // This will be handled by MainWindow.xaml.cs
        OnSelectWholeLineRequested?.Invoke();
    }

    [RelayCommand]
    private void SelectAll()
    {
        // This will be handled by MainWindow.xaml.cs
        OnSelectAllRequested?.Invoke();
    }

    public event Action? OnSelectWholeLineRequested;
    public event Action? OnSelectAllRequested;

    [RelayCommand]
    private async Task OpenMultipleFilesAsync()
    {
        if (_multiFileLogService == null)
        {
            WpfMessageBox.Show("Multi-file support is not enabled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Log Files (*.log;*.txt)|*.log;*.txt|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Open Multiple Log Files",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            await LoadMultipleFilesAsync(dialog.FileNames);
        }
    }

    private async Task LoadMultipleFilesAsync(string[] filePaths)
    {
        IsLoading = true;
        StatusText = $"Loading {filePaths.Length} files...";
        _isMultiFileMode = true;

        try
        {
            var combinedText = await _multiFileLogService!.LoadFilesAsync(filePaths);
            TotalLogCount = _logService.GetTotalLineCount(combinedText);

            OriginalLogText = combinedText;
            LogText = combinedText;

            var fileNames = string.Join(", ", filePaths.Select(Path.GetFileName));
            StatusText = $"Loaded {filePaths.Length} files - {TotalLogCount:N0} total lines";
            LoadedFilesInfo = $"Files: {fileNames}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading files: {ex.Message}";
            WpfMessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        if (_multiFileLogService == null)
        {
            WpfMessageBox.Show("Multi-file support is not enabled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "Select folder containing log files",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            await LoadFolderAsync(dialog.SelectedPath);
        }
    }

    private async Task LoadFolderAsync(string folderPath)
    {
        if (_multiFileLogService == null) return;

        IsLoading = true;
        StatusText = $"Loading folder: {Path.GetFileName(folderPath)}...";
        _isMultiFileMode = true;

        try
        {
            var combinedText = await _multiFileLogService.LoadFolderAsync(folderPath, "*.log");
            TotalLogCount = _logService.GetTotalLineCount(combinedText);

            OriginalLogText = combinedText;
            LogText = combinedText;

            var loadedFiles = _multiFileLogService.GetLoadedFiles();
            LoadedFilesInfo = $"{loadedFiles.Count} files from folder";
            StatusText = $"Loaded {TotalLogCount:N0} lines from {loadedFiles.Count} files in folder";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading folder: {ex.Message}";
            WpfMessageBox.Show($"Error loading folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveFilterAsync()
    {
        var filterName = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter a name for this filter configuration:",
            "Save Filter",
            $"Filter_{DateTime.Now:yyyyMMdd_HHmmss}");

        if (string.IsNullOrWhiteSpace(filterName))
            return;

        var filter = new FilterConfiguration
        {
            Name = filterName,
            LogLevel = FilterLevel,
            StartTime = FilterStartTime,
            EndTime = FilterEndTime,
            SearchText = SearchText
        };

        SavedFilters.Add(filter);
        await SaveFiltersToFileAsync();
        StatusText = $"Filter '{filterName}' saved";
    }

    [RelayCommand]
    private async Task LoadFilterAsync()
    {
        if (SelectedFilter == null)
            return;

        FilterLevel = SelectedFilter.LogLevel;
        FilterStartTime = SelectedFilter.StartTime;
        FilterEndTime = SelectedFilter.EndTime;
        SearchText = SelectedFilter.SearchText ?? string.Empty;

        StatusText = $"Loaded filter: {SelectedFilter.Name}";
        await ApplyFilterAsync();
    }

    [RelayCommand]
    private async Task DeleteFilterAsync()
    {
        if (SelectedFilter == null)
            return;

        var result = WpfMessageBox.Show(
            $"Delete filter '{SelectedFilter.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            SavedFilters.Remove(SelectedFilter);
            await SaveFiltersToFileAsync();
            StatusText = "Filter deleted";
        }
    }

    [RelayCommand]
    private async Task ExportFiltersAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            FileName = "filters.json",
            Title = "Export Filter Configurations"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var collection = new FilterConfigurationCollection
                {
                    Filters = SavedFilters.ToList()
                };
                await _filterConfigService.SaveAsync(collection, dialog.FileName);
                StatusText = "Filters exported successfully";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error exporting filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task ImportFiltersAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = "Import Filter Configurations"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var collection = await _filterConfigService.LoadAsync(dialog.FileName);
                foreach (var filter in collection.Filters)
                {
                    if (!SavedFilters.Any(f => f.Name == filter.Name))
                    {
                        SavedFilters.Add(filter);
                    }
                }
                await SaveFiltersToFileAsync();
                StatusText = $"Imported {collection.Filters.Count} filters";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error importing filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task LoadSavedFiltersAsync()
    {
        try
        {
            var filtersPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LogViewer2026",
                "filters.json");

            if (File.Exists(filtersPath))
            {
                var collection = await _filterConfigService.LoadAsync(filtersPath);
                SavedFilters.Clear();
                foreach (var filter in collection.Filters)
                {
                    SavedFilters.Add(filter);
                }
            }
            else
            {
                var defaults = await _filterConfigService.GetDefaultAsync();
                foreach (var filter in defaults.Filters)
                {
                    SavedFilters.Add(filter);
                }
            }
        }
        catch
        {
            // Ignore errors loading filters
        }
    }

    private async Task SaveFiltersToFileAsync()
    {
        try
        {
            var filtersPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LogViewer2026",
                "filters.json");

            var collection = new FilterConfigurationCollection
            {
                Filters = SavedFilters.ToList()
            };

            await _filterConfigService.SaveAsync(collection, filtersPath);
        }
        catch
        {
            // Ignore errors saving filters
        }
    }

    public void Dispose()
    {
        _logService.Dispose();
        _multiFileLogService?.Dispose();
    }
}
