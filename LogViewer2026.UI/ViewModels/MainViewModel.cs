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
#pragma warning disable CS0414 // Field is assigned but its value is never used - reserved for future multi-file mode functionality
    private bool _isMultiFileMode;
#pragma warning restore CS0414
    private int _cachedContextLines = 5; // Cache the setting
    private List<int> _originalLineOffsets = new(); // Pre-built line start offsets for O(1) access

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
    private ObservableCollection<string> _loadedFileNames = [];

    [ObservableProperty]
    private string? _selectedDisplayFile;

    [ObservableProperty]
    private bool _hasMultipleFiles;

    [ObservableProperty]
    private bool _showSelectedFileOnly;

    private string? _activeFileText;

    public event Action<string>? OnGoToFileRequested;

    partial void OnSelectedDisplayFileChanged(string? value)
    {
        if (value == null) return;
        OnGoToFileRequested?.Invoke(value);
        if (ShowSelectedFileOnly)
            UpdateFileFilter();
    }

    partial void OnShowSelectedFileOnlyChanged(bool value)
    {
        UpdateFileFilter();
    }

    [RelayCommand]
    private void GoToSelectedFile()
    {
        if (!string.IsNullOrEmpty(SelectedDisplayFile))
            OnGoToFileRequested?.Invoke(SelectedDisplayFile);
    }

    private void UpdateFileFilter()
    {
        _activeFileText = ShowSelectedFileOnly && !string.IsNullOrEmpty(SelectedDisplayFile)
            ? ExtractFileContent(SelectedDisplayFile!)
            : null;

        if (FilterLevel != null || FilterStartTime.HasValue || FilterEndTime.HasValue)
            _ = ApplyFilterAsync();
        else if (FilterSearchResults && !string.IsNullOrEmpty(SearchText))
            ApplySearchFilter();
        else
            LogText = _activeFileText ?? OriginalLogText;
    }

    private string ExtractFileContent(string fileName)
    {
        var header = $"=== File: {fileName} ===";
        var text = OriginalLogText;
        var startIdx = text.IndexOf(header, StringComparison.Ordinal);
        if (startIdx < 0) return text;
        var nextIdx = text.IndexOf("=== File:", startIdx + header.Length, StringComparison.Ordinal);
        return nextIdx < 0
            ? text[startIdx..].TrimEnd()
            : text[startIdx..nextIdx].TrimEnd();
    }

    [ObservableProperty]
    private LookingGlassData _selectedLookingGlas = new LookingGlassData();

    [ObservableProperty]
    private bool _autoUpdateLookingGlass = false;

    [ObservableProperty]
    private bool _filterSearchResults = false;

    [ObservableProperty]
    private bool _showLookingGlass = true;

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
        // Filter will be applied when user clicks Apply button
    }

    partial void OnFilterStartTimeChanged(DateTime? value)
    {
        // Filter will be applied when user clicks Apply button
    }

    partial void OnFilterEndTimeChanged(DateTime? value)
    {
        // Filter will be applied when user clicks Apply button
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

        // Load settings cache
        _ = LoadSettingsCacheAsync();

        _ = LoadSavedFiltersAsync();
    }

    private async Task LoadSettingsCacheAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            _cachedContextLines = settings.LookingGlassContextLines;
            AutoUpdateLookingGlass = settings.AutoUpdateLookingGlass;
            FilterSearchResults = settings.FilterSearchResults;
            ShowLookingGlass = settings.ShowLookingGlass;
        }
        catch
        {
            _cachedContextLines = 5; // Default
            AutoUpdateLookingGlass = false; // Default
            FilterSearchResults = false; // Default
            ShowLookingGlass = true; // Default
        }
    }

    public void RefreshSettingsCache()
    {
        _ = LoadSettingsCacheAsync();
    }

    partial void OnOriginalLogTextChanged(string value)
    {
        BuildLineIndex(value);
    }

    private void BuildLineIndex(string text)
    {
        _originalLineOffsets.Clear();
        if (string.IsNullOrEmpty(text)) return;

        _originalLineOffsets.Add(0);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                _originalLineOffsets.Add(i + 1);
        }
    }

    private string GetOriginalLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _originalLineOffsets.Count)
            return string.Empty;

        var start = _originalLineOffsets[lineIndex];
        int end;

        if (lineIndex + 1 < _originalLineOffsets.Count)
            end = _originalLineOffsets[lineIndex + 1] - 1; // -1 to exclude \n
        else
            end = OriginalLogText.Length;

        // Handle \r\n
        if (end > start && end <= OriginalLogText.Length && OriginalLogText[end - 1] == '\r')
            end--;

        return OriginalLogText.Substring(start, end - start);
    }

    private int FindOriginalLineNumber(string lineText)
    {
        var span = lineText.AsSpan();
        for (int i = 0; i < _originalLineOffsets.Count; i++)
        {
            var start = _originalLineOffsets[i];
            int end;
            if (i + 1 < _originalLineOffsets.Count)
                end = _originalLineOffsets[i + 1] - 1;
            else
                end = OriginalLogText.Length;

            if (end > start && end <= OriginalLogText.Length && OriginalLogText[end - 1] == '\r')
                end--;

            var lineLen = end - start;
            if (lineLen == span.Length && OriginalLogText.AsSpan(start, lineLen).SequenceEqual(span))
                return i;
        }
        return -1;
    }

    public async Task SaveAutoUpdateSettingAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            settings.AutoUpdateLookingGlass = AutoUpdateLookingGlass;
            await _settingsService.SaveAsync(settings);
        }
        catch
        {
            // Ignore errors
        }
    }

    public async Task SaveFilterSearchSettingAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            settings.FilterSearchResults = FilterSearchResults;
            await _settingsService.SaveAsync(settings);
        }
        catch
        {
            // Ignore errors
        }
    }

    public async Task SaveShowLookingGlassSettingAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            settings.ShowLookingGlass = ShowLookingGlass;
            await _settingsService.SaveAsync(settings);
        }
        catch
        {
            // Ignore errors
        }
    }

    public void ApplySearchFilter()
    {
        if (string.IsNullOrEmpty(SearchText) || !FilterSearchResults)
        {
            // If no search text or filter disabled, restore filtered/original text based on level filter
            if (FilterLevel == null)
            {
                LogText = _activeFileText ?? OriginalLogText;
                StatusText = $"Showing all {TotalLogCount:N0} lines";
            }
            // Note: Don't reapply level filter here to avoid recursion
            // The level filter is already applied if FilterLevel is set
            return;
        }

        // Single-pass: apply both level and search filters without Split/Join
        var text = _activeFileText ?? OriginalLogText;
        var sb = new System.Text.StringBuilder();
        int filteredCount = 0;
        int lineStart = 0;
        var searchSpan = SearchText.AsSpan();

        for (int i = 0; i <= text.Length; i++)
        {
            if (i == text.Length || text[i] == '\n')
            {
                var lineSpan = text.AsSpan(lineStart, i - lineStart);

                bool passesLevel = FilterLevel == null || ContainsLogLevel(lineSpan, FilterLevel.Value);
                bool passesSearch = lineSpan.Contains(searchSpan, StringComparison.OrdinalIgnoreCase);

                if (passesLevel && passesSearch)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(lineSpan);
                    filteredCount++;
                }

                lineStart = i + 1;
            }
        }

        LogText = sb.ToString();

        // Update status to show both filters if applicable
        if (FilterLevel == null)
        {
            StatusText = $"Showing {filteredCount:N0} lines matching '{SearchText}'";
        }
        else
        {
            StatusText = $"Showing {filteredCount:N0} lines with level {FilterLevel} matching '{SearchText}'";
        }
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            ShowHiddenItems = true,            
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
            LoadedFileNames.Clear();
            HasMultipleFiles = false;
            SelectedDisplayFile = null;
            ShowSelectedFileOnly = false;
            _activeFileText = null;
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
    public event Action<string, string>? OnScrollToLine; // lineContent, selectedText

    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        if (string.IsNullOrEmpty(OriginalLogText))
        {
            StatusText = "No log file loaded";
            return;
        }

        // Save the current line content and selected text to restore position after filtering
        var currentLineToPreserve = CurrentLine;
        var selectedTextToPreserve = SelectedText;

        IsLoading = true;
        StatusText = "Applying filter...";

        try
        {
            await Task.Run(() =>
            {
                var text = _activeFileText ?? OriginalLogText;
                var sb = new System.Text.StringBuilder();
                int filteredCount = 0;
                int lineStart = 0;

                for (int i = 0; i <= text.Length; i++)
                {
                    if (i == text.Length || text[i] == '\n')
                    {
                        var lineSpan = text.AsSpan(lineStart, i - lineStart);

                        // Apply all filters: log level AND date/time range
                        bool passesLevelFilter = FilterLevel == null || ContainsLogLevel(lineSpan, FilterLevel.Value);
                        bool passesDateFilter = PassesDateTimeFilter(lineSpan);

                        if (passesLevelFilter && passesDateFilter)
                        {
                            if (sb.Length > 0) sb.Append('\n');
                            sb.Append(lineSpan);
                            filteredCount++;
                        }

                        lineStart = i + 1;
                    }
                }

                var filteredText = sb.ToString();

                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    LogText = filteredText;

                    // Build status message
                    var statusParts = new List<string>();

                    if (FilterLevel != null)
                        statusParts.Add($"level: {FilterLevel}");

                    if (FilterStartTime.HasValue || FilterEndTime.HasValue)
                    {
                        if (FilterStartTime.HasValue && FilterEndTime.HasValue)
                            statusParts.Add($"date: {FilterStartTime.Value:yyyy-MM-dd HH:mm} to {FilterEndTime.Value:yyyy-MM-dd HH:mm}");
                        else if (FilterStartTime.HasValue)
                            statusParts.Add($"date: from {FilterStartTime.Value:yyyy-MM-dd HH:mm}");
                        else if (FilterEndTime.HasValue)
                            statusParts.Add($"date: until {FilterEndTime.Value:yyyy-MM-dd HH:mm}");
                    }

                    if (statusParts.Count > 0)
                        StatusText = $"Filtered to {filteredCount:N0} lines with {string.Join(" and ", statusParts)}";
                    else
                        StatusText = $"Showing all {filteredCount:N0} lines";

                    // Try to restore position to the same line with selection
                    if (!string.IsNullOrEmpty(currentLineToPreserve))
                    {
                        OnScrollToLine?.Invoke(currentLineToPreserve, selectedTextToPreserve ?? string.Empty);
                    }

                    // Apply search filter if enabled
                    if (FilterSearchResults && !string.IsNullOrEmpty(SearchText))
                    {
                        ApplySearchFilter();
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

    private static readonly Dictionary<LogLevel, string[]> _logLevelPatterns = BuildLogLevelPatterns();

    private static Dictionary<LogLevel, string[]> BuildLogLevelPatterns()
    {
        var result = new Dictionary<LogLevel, string[]>();

        foreach (var level in Enum.GetValues<LogLevel>())
        {
            var levelStr = level.ToString();
            var patterns = new List<string>
            {
                $"[{levelStr}]", $"[{levelStr.ToUpper()}]", $"[{levelStr.ToLower()}]",
                $" {levelStr} ", $" {levelStr.ToUpper()} ", $" {levelStr.ToLower()} ",
                $":{levelStr}:", $":{levelStr.ToUpper()}:", $":{levelStr.ToLower()}:",
            };

            switch (level)
            {
                case LogLevel.Information:
                    patterns.AddRange(["[INF]", "[INFO]", " INF ", " INFO "]);
                    break;
                case LogLevel.Warning:
                    patterns.AddRange(["[WRN]", "[WARN]", " WRN ", " WARN "]);
                    break;
                case LogLevel.Error:
                    patterns.AddRange(["[ERR]", " ERR "]);
                    break;
                case LogLevel.Debug:
                    patterns.AddRange(["[DBG]", " DBG "]);
                    break;
                case LogLevel.Verbose:
                    patterns.AddRange(["[VRB]", " VRB ", "[TRACE]", " TRACE "]);
                    break;
                case LogLevel.Fatal:
                    patterns.AddRange(["[FTL]", " FTL ", "[CRITICAL]", " CRITICAL "]);
                    break;
            }

            result[level] = patterns.ToArray();
        }

        return result;
    }

    private static bool ContainsLogLevel(string line, LogLevel level)
    {
        return ContainsLogLevel(line.AsSpan(), level);
    }

    private static bool ContainsLogLevel(ReadOnlySpan<char> line, LogLevel level)
    {
        if (!_logLevelPatterns.TryGetValue(level, out var patterns))
            return false;

        foreach (var pattern in patterns)
        {
            if (line.Contains(pattern.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static DateTime? TryExtractTimestamp(ReadOnlySpan<char> line)
    {
        // Common log timestamp patterns at the start of lines:
        // 2024-01-15 10:30:45
        // 2024-01-15T10:30:45
        // [2024-01-15 10:30:45]
        // 2024-01-15 10:30:45.123
        // 15/01/2024 10:30:45

        if (line.Length < 10)
            return null;

        // Try to find a date-like pattern (YYYY-MM-DD or DD/MM/YYYY)
        for (int i = 0; i < Math.Min(line.Length - 10, 30); i++)
        {
            var segment = line.Slice(i, Math.Min(30, line.Length - i));

            // Try to parse various datetime formats
            var segmentString = segment.ToString();

            // Try ISO format first (most common in logs)
            if (DateTime.TryParseExact(segmentString.Substring(0, Math.Min(19, segmentString.Length)), 
                new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd" }, 
                null, 
                System.Globalization.DateTimeStyles.None, 
                out var dt))
            {
                return dt;
            }

            // Try with milliseconds
            if (segmentString.Length >= 23 && DateTime.TryParseExact(segmentString.Substring(0, 23), 
                "yyyy-MM-dd HH:mm:ss.fff", 
                null, 
                System.Globalization.DateTimeStyles.None, 
                out dt))
            {
                return dt;
            }

            // Try general parsing as fallback
            if (DateTime.TryParse(segmentString.Substring(0, Math.Min(20, segmentString.Length)), out dt))
            {
                // Sanity check - log dates should be reasonable (between 2000 and 2100)
                if (dt.Year >= 2000 && dt.Year <= 2100)
                {
                    return dt;
                }
            }
        }

        return null;
    }

    private bool PassesDateTimeFilter(ReadOnlySpan<char> line)
    {
        // If no date filter is set, pass all lines
        if (FilterStartTime == null && FilterEndTime == null)
            return true;

        var timestamp = TryExtractTimestamp(line);

        // If we couldn't extract a timestamp, include the line (might be multi-line log continuation)
        if (timestamp == null)
            return true;

        // Check if timestamp is within the range
        if (FilterStartTime.HasValue && timestamp.Value < FilterStartTime.Value)
            return false;

        if (FilterEndTime.HasValue && timestamp.Value > FilterEndTime.Value)
            return false;

        return true;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        // Save the current line content and selected text to restore position after clearing filters
        var currentLineToPreserve = CurrentLine;
        var selectedTextToPreserve = SelectedText;

        SelectedLogLevelOption = AvailableLogLevels.First(); // Reset to "All"
        FilterLevel = null;
        FilterStartTime = null;
        FilterEndTime = null;
        SearchText = string.Empty;

        // Restore original text
        if (!string.IsNullOrEmpty(OriginalLogText))
        {
            LogText = _activeFileText ?? OriginalLogText;
            StatusText = $"Filters cleared - showing all {TotalLogCount:N0} lines";

            // Try to restore position to the same line with selection
            if (!string.IsNullOrEmpty(currentLineToPreserve))
            {
                OnScrollToLine?.Invoke(currentLineToPreserve, selectedTextToPreserve ?? string.Empty);
            }
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

    [RelayCommand]
    private void UpdateLookingGlassView()
    {
        // This will be handled by MainWindow.xaml.cs
        OnUpdateLookingGlassRequested?.Invoke();
    }

    public event Action? OnSelectWholeLineRequested;
    public event Action? OnSelectAllRequested;
    public event Action? OnUpdateLookingGlassRequested;

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
            ShowHiddenItems = true,
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

            LoadedFileNames.Clear();
            foreach (var f in _multiFileLogService!.GetLoadedFiles())
                LoadedFileNames.Add(Path.GetFileName(f)!);
            HasMultipleFiles = LoadedFileNames.Count > 1;
            SelectedDisplayFile = null;
            ShowSelectedFileOnly = false;
            _activeFileText = null;
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

            LoadedFileNames.Clear();
            foreach (var f in loadedFiles)
                LoadedFileNames.Add(Path.GetFileName(f)!);
            HasMultipleFiles = LoadedFileNames.Count > 1;
            SelectedDisplayFile = null;
            ShowSelectedFileOnly = false;
            _activeFileText = null;
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
                await _filterConfigService.SaveAsync(collection, dialog.FileName, CancellationToken.None);
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
                var collection = await _filterConfigService.LoadAsync(dialog.FileName, CancellationToken.None);
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
                var collection = await _filterConfigService.LoadAsync(filtersPath, CancellationToken.None);
                SavedFilters.Clear();
                foreach (var filter in collection.Filters)
                {
                    SavedFilters.Add(filter);
                }
            }
            else
            {
                var defaults = await _filterConfigService.GetDefaultAsync(CancellationToken.None);
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

            await _filterConfigService.SaveAsync(collection, filtersPath, CancellationToken.None);
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

    public void UpdateLookingGlass(int selectedLineNumber, int selectedStartOffset, int selectedLength, string currentDisplayedText)
    {
        if (string.IsNullOrEmpty(OriginalLogText))
        {
            SelectedLookingGlas.Text = string.Empty;
            return;
        }

        // Use cached context lines setting
        var contextLines = _cachedContextLines;

        // Calculate selection end position to determine which lines are selected
        var selectedEndOffset = selectedStartOffset + selectedLength;

        // Get line from displayed text using indexing instead of Split
        int displayedLineStart = 0;
        int currentDisplayedLineNumber = 1;
        string currentLineText = string.Empty;
        int selectionEndLineNumber = selectedLineNumber; // Track where selection ends

        // First pass: find the line at selection start
        for (int i = 0; i <= currentDisplayedText.Length; i++)
        {
            if (i == currentDisplayedText.Length || currentDisplayedText[i] == '\n')
            {
                if (currentDisplayedLineNumber == selectedLineNumber)
                {
                    int end = i;
                    if (end > displayedLineStart && currentDisplayedText[end - 1] == '\r')
                        end--;
                    currentLineText = currentDisplayedText.Substring(displayedLineStart, end - displayedLineStart);
                    break;
                }
                displayedLineStart = i + 1;
                currentDisplayedLineNumber++;
            }
        }

        // Second pass: find the line at selection end (if multi-line selection)
        if (selectedLength > 0)
        {
            displayedLineStart = 0;
            currentDisplayedLineNumber = 1;
            for (int i = 0; i <= currentDisplayedText.Length; i++)
            {
                if (i == currentDisplayedText.Length || currentDisplayedText[i] == '\n')
                {
                    // Check if selection end is within this line
                    int lineEnd = i;
                    if (selectedEndOffset >= displayedLineStart && selectedEndOffset <= lineEnd)
                    {
                        selectionEndLineNumber = currentDisplayedLineNumber;
                        break;
                    }
                    displayedLineStart = i + 1;
                    currentDisplayedLineNumber++;
                }
            }
        }

        if (string.IsNullOrEmpty(currentLineText))
        {
            SelectedLookingGlas.Text = string.Empty;
            return;
        }

        // Calculate offset within the line for the selection start and end
        int lineStartOffset = 0;
        for (int i = 0; i < selectedLineNumber - 1 && lineStartOffset < currentDisplayedText.Length; i++)
        {
            int nextNewline = currentDisplayedText.IndexOf('\n', lineStartOffset);
            if (nextNewline < 0) break;
            lineStartOffset = nextNewline + 1;
        }
        var offsetInLine = selectedStartOffset - lineStartOffset;

        // Calculate offset at selection end (for multi-line selections)
        int selectionEndLineStartOffset = 0;
        for (int i = 0; i < selectionEndLineNumber - 1 && selectionEndLineStartOffset < currentDisplayedText.Length; i++)
        {
            int nextNewline = currentDisplayedText.IndexOf('\n', selectionEndLineStartOffset);
            if (nextNewline < 0) break;
            selectionEndLineStartOffset = nextNewline + 1;
        }
        var offsetInEndLine = selectedEndOffset - selectionEndLineStartOffset;

        // Find this line in the original text using the index
        int originalLineNumber = FindOriginalLineNumber(currentLineText);

        if (originalLineNumber == -1)
        {
            // Line not found in original text, just show the current line with context from displayed text
            var sb = new System.Text.StringBuilder();
            int displayLineNum = 1;
            int lineStart = 0;
            int startLine = Math.Max(1, selectedLineNumber - contextLines);
            int endLine = Math.Min(currentDisplayedLineNumber, selectedLineNumber + contextLines);
            int newHighlightStartOffset = 0;
            int newHighlightEndOffset = 0;

            for (int i = 0; i <= currentDisplayedText.Length; i++)
            {
                if (i == currentDisplayedText.Length || currentDisplayedText[i] == '\n')
                {
                    if (displayLineNum >= startLine && displayLineNum <= endLine)
                    {
                        if (sb.Length > 0) sb.Append('\n');

                        if (displayLineNum == selectedLineNumber)
                        {
                            newHighlightStartOffset = sb.Length + Math.Max(0, offsetInLine);
                        }

                        if (displayLineNum == selectionEndLineNumber)
                        {
                            newHighlightEndOffset = sb.Length + Math.Max(0, offsetInEndLine);
                        }

                        int end = i;
                        if (end > lineStart && currentDisplayedText[end - 1] == '\r')
                            end--;
                        sb.Append(currentDisplayedText.AsSpan(lineStart, end - lineStart));
                    }

                    displayLineNum++;
                    if (displayLineNum > endLine) break;
                    lineStart = i + 1;
                }
            }

            SelectedLookingGlas.Text = sb.ToString();
            SelectedLookingGlas.HighlightStartOffset = newHighlightStartOffset;
            // Calculate actual highlight length from the Looking Glass text structure
            SelectedLookingGlas.HighlightLength = Math.Max(0, 
                Math.Min(newHighlightEndOffset, sb.Length) - newHighlightStartOffset);
            SelectedLookingGlas.StartingLineNumber = startLine;
            return;
        }

        // Use indexed access to original text
        var originalStartLine = Math.Max(0, originalLineNumber - contextLines);
        var originalEndLine = Math.Min(_originalLineOffsets.Count - 1, originalLineNumber + contextLines);

        // Find the end line in the original text (might be different if selection spans multiple lines)
        var selectionEndLineText = string.Empty;
        int endLineStartOffset = 0;
        for (int i = 0; i < selectionEndLineNumber - 1 && endLineStartOffset < currentDisplayedText.Length; i++)
        {
            int nextNewline = currentDisplayedText.IndexOf('\n', endLineStartOffset);
            if (nextNewline < 0) break;
            endLineStartOffset = nextNewline + 1;
        }
        // Extract the selection end line text
        for (int i = endLineStartOffset; i <= currentDisplayedText.Length; i++)
        {
            if (i == currentDisplayedText.Length || currentDisplayedText[i] == '\n')
            {
                int end = i;
                if (end > endLineStartOffset && currentDisplayedText[end - 1] == '\r')
                    end--;
                selectionEndLineText = currentDisplayedText.Substring(endLineStartOffset, end - endLineStartOffset);
                break;
            }
        }
        int originalSelectionEndLineNumber = selectionEndLineNumber == selectedLineNumber 
            ? originalLineNumber 
            : FindOriginalLineNumber(selectionEndLineText);
        if (originalSelectionEndLineNumber == -1)
            originalSelectionEndLineNumber = originalLineNumber; // Fallback to start line

        var originalContextText = new System.Text.StringBuilder();
        int highlightOffset = 0;
        int highlightEndOffset = 0;

        for (int i = originalStartLine; i <= originalEndLine; i++)
        {
            if (originalContextText.Length > 0)
                originalContextText.Append('\n');

            if (i == originalLineNumber)
            {
                highlightOffset = originalContextText.Length + Math.Max(0, offsetInLine);
            }

            if (i == originalSelectionEndLineNumber)
            {
                highlightEndOffset = originalContextText.Length + Math.Max(0, offsetInEndLine);
            }

            originalContextText.Append(GetOriginalLine(i));
        }

        SelectedLookingGlas.Text = originalContextText.ToString();
        SelectedLookingGlas.HighlightStartOffset = highlightOffset;
        // Calculate actual highlight length from the Looking Glass text structure
        SelectedLookingGlas.HighlightLength = Math.Max(0, 
            Math.Min(highlightEndOffset, originalContextText.Length) - highlightOffset);
        SelectedLookingGlas.StartingLineNumber = originalStartLine + 1; // Line numbers are 1-based
    }



    public class LookingGlassData : ObservableObject
    {
        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        private int _highlightStartOffset = -1;
        public int HighlightStartOffset
        {
            get => _highlightStartOffset;
            set => SetProperty(ref _highlightStartOffset, value);
        }

        private int _highlightLength = 0;
        public int HighlightLength
        {
            get => _highlightLength;
            set => SetProperty(ref _highlightLength, value);
        }

        private int _startingLineNumber = 1;
        public int StartingLineNumber
        {
            get => _startingLineNumber;
            set => SetProperty(ref _startingLineNumber, value);
        }
    }
}
