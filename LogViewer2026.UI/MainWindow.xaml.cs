using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LogViewer2026.UI.ViewModels;
using LogViewer2026.UI.Highlighting;
using LogViewer2026.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using WpfApp = System.Windows.Application;

namespace LogViewer2026.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SearchResultHighlighter _searchHighlighter;
    private readonly OffsetLineNumberMargin _lookingGlassLineNumberMargin;
    private bool _isLookingGlassContextMenuActive = false;
    private int _lastLogEditorLineNumber = -1;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Set up AvalonEdit
        LogEditor.SyntaxHighlighting = LogHighlighting.CreateLogHighlighting();
        LogLookingGlas.SyntaxHighlighting = LogHighlighting.CreateLogHighlighting();

        // Replace default line number margin with custom offset margin for LogLookingGlas
        LogLookingGlas.ShowLineNumbers = false; // Disable default line numbers
        _lookingGlassLineNumberMargin = new OffsetLineNumberMargin();
        LogLookingGlas.TextArea.LeftMargins.Insert(0, _lookingGlassLineNumberMargin);

        // Set up search result highlighting
        _searchHighlighter = new SearchResultHighlighter();
        LogEditor.TextArea.TextView.LineTransformers.Add(_searchHighlighter);

        // Bind ViewModel properties to AvalonEdit
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Handle search result navigation
        _viewModel.OnSearchResultChanged += NavigateToSearchResult;

        // Handle scroll to line (for preserving position after filter)
        _viewModel.OnScrollToLine += ScrollToLine;

        // Handle editor actions
        _viewModel.OnSelectWholeLineRequested += SelectWholeLine;
        _viewModel.OnSelectAllRequested += SelectAll;

        // Handle text selection and update CurrentLine
        LogEditor.TextArea.SelectionChanged += (s, e) =>
        {
            _viewModel.SelectedText = LogEditor.SelectedText;
            UpdateCurrentLine();

            // Synchronize selection with LogLookingGlas
            SyncSelectionToLookingGlass();
        };

        // Update current line on caret position change and auto-update looking glass if line changed
        LogEditor.TextArea.Caret.PositionChanged += (s, e) =>
        {
            UpdateCurrentLine();

            // Auto-update looking glass if enabled and the line number has changed
            if (_viewModel.AutoUpdateLookingGlass && LogEditor.Document != null)
            {
                var currentLineNumber = LogEditor.Document.GetLineByOffset(LogEditor.CaretOffset).LineNumber;

                if (currentLineNumber != _lastLogEditorLineNumber)
                {
                    _lastLogEditorLineNumber = currentLineNumber;
                    UpdateLookingGlass();
                }
            }
        };

        // Handle text selection in LogLookingGlas
        LogLookingGlas.TextArea.SelectionChanged += (s, e) =>
        {
            // Update SelectedText from LogLookingGlas when selecting there
            if (LogLookingGlas.IsFocused)
            {
                _viewModel.SelectedText = LogLookingGlas.SelectedText;
                UpdateCurrentLineFromLookingGlass();
            }
        };

        // Update current line on caret position change in LogLookingGlas
        LogLookingGlas.TextArea.Caret.PositionChanged += (s, e) =>
        {
            if (LogLookingGlas.IsFocused)
            {
                UpdateCurrentLineFromLookingGlass();
            }
        };

        // Handle manual looking glass update
        _viewModel.OnUpdateLookingGlassRequested += UpdateLookingGlass;

        // Keyboard shortcuts
        var findNextGesture = new KeyGesture(Key.F3);
        var findPreviousGesture = new KeyGesture(Key.F3, ModifierKeys.Shift);
        var selectLineGesture = new KeyGesture(Key.L, ModifierKeys.Control);

        InputBindings.Add(new KeyBinding(_viewModel.FindNextCommand, findNextGesture));
        InputBindings.Add(new KeyBinding(_viewModel.FindPreviousCommand, findPreviousGesture));
        InputBindings.Add(new KeyBinding(_viewModel.SelectWholeLineCommand, selectLineGesture));

        // Focus search box on Ctrl+F
        var findGesture = new KeyGesture(Key.F, ModifierKeys.Control);
        InputBindings.Add(new InputBinding(new RelayCommand(() => SearchBox.Focus()), findGesture));

        // Set initial row height based on ShowLookingGlass setting
        Loaded += (s, e) => UpdateLookingGlassRowHeight();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.LogText))
        {
            Dispatcher.Invoke(() =>
            {
                // Set entire text at once - AvalonEdit handles it efficiently!
                LogEditor.Text = _viewModel.LogText;

                // Update search highlighting
                if (!string.IsNullOrEmpty(_viewModel.SearchText))
                {
                    UpdateSearchHighlighting();
                }

                // Scroll to end
                if (!string.IsNullOrEmpty(_viewModel.LogText))
                {
                    LogEditor.ScrollToEnd();
                }
            });
        }
        else if (e.PropertyName == nameof(MainViewModel.SearchText))
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(_viewModel.SearchText))
                {
                    // Clear highlighting
                    _searchHighlighter.SearchTerm = string.Empty;
                    _viewModel.TotalSearchResults = 0;
                    _viewModel.CurrentSearchResultIndex = -1;
                    LogEditor.TextArea.TextView.Redraw();

                    // Clear search filter if enabled
                    if (_viewModel.FilterSearchResults)
                    {
                        _viewModel.ApplySearchFilter();
                    }
                }
                else
                {
                    UpdateSearchHighlighting();

                    // Apply search filter if enabled
                    if (_viewModel.FilterSearchResults)
                    {
                        _viewModel.ApplySearchFilter();
                    }
                }
            });
        }
    }

    private void UpdateCurrentLine()
    {
        if (LogEditor.Document == null)
            return;

        var line = LogEditor.Document.GetLineByOffset(LogEditor.CaretOffset);
        _viewModel.CurrentLine = LogEditor.Document.GetText(line.Offset, line.Length);
    }

    private void SyncSelectionToLookingGlass()
    {
        // Only sync if there's a selection and LogLookingGlas is visible and has content
        if (string.IsNullOrEmpty(LogEditor.SelectedText) || 
            LogLookingGlas.Document == null || 
            string.IsNullOrEmpty(LogLookingGlas.Text) ||
            !_viewModel.ShowLookingGlass)
        {
            return;
        }

        try
        {
            var selectedText = LogEditor.SelectedText;
            var lookingGlassText = LogLookingGlas.Text;

            // Find the selected text in LogLookingGlas
            var index = lookingGlassText.IndexOf(selectedText, StringComparison.Ordinal);

            if (index >= 0)
            {
                // Found the text, select it in LogLookingGlas
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        LogLookingGlas.Select(index, selectedText.Length);

                        // Scroll to make the selection visible
                        if (LogLookingGlas.Document != null)
                        {
                            var location = LogLookingGlas.Document.GetLocation(index);
                            LogLookingGlas.ScrollTo(location.Line, location.Column);
                        }
                    }
                    catch
                    {
                        // Ignore any errors during selection sync
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        catch
        {
            // Ignore any errors during selection sync
        }
    }

    private void UpdateCurrentLineFromLookingGlass()
    {
        if (LogLookingGlas.Document == null)
            return;

        var line = LogLookingGlas.Document.GetLineByOffset(LogLookingGlas.CaretOffset);
        _viewModel.CurrentLine = LogLookingGlas.Document.GetText(line.Offset, line.Length);
    }

    private void UpdateLookingGlass()
    {
        if (LogEditor.Document == null || string.IsNullOrEmpty(_viewModel.OriginalLogText))
            return;

        var line = LogEditor.Document.GetLineByOffset(LogEditor.CaretOffset);
        var lineNumber = line.LineNumber;
        var selectedLength = LogEditor.SelectionLength;

        // Update the looking glass with context
        _viewModel.UpdateLookingGlass(lineNumber, LogEditor.SelectionStart, selectedLength, LogEditor.Text);

        // Update the looking glass editor
        Dispatcher.Invoke(() =>
        {
            LogLookingGlas.Text = _viewModel.SelectedLookingGlas.Text;

            // Update line number offset to match original line numbers
            _lookingGlassLineNumberMargin.LineNumberOffset = _viewModel.SelectedLookingGlas.StartingLineNumber - 1;

            // Highlight selected text if any
            if (_viewModel.SelectedLookingGlas.HighlightLength > 0 && 
                _viewModel.SelectedLookingGlas.HighlightStartOffset >= 0 &&
                _viewModel.SelectedLookingGlas.HighlightStartOffset < LogLookingGlas.Text.Length)
            {
                var highlightLength = Math.Min(
                    _viewModel.SelectedLookingGlas.HighlightLength,
                    LogLookingGlas.Text.Length - _viewModel.SelectedLookingGlas.HighlightStartOffset);

                LogLookingGlas.Select(_viewModel.SelectedLookingGlas.HighlightStartOffset, highlightLength);

                // Scroll to the highlighted text
                if (LogLookingGlas.Document != null)
                {
                    var location = LogLookingGlas.Document.GetLocation(_viewModel.SelectedLookingGlas.HighlightStartOffset);
                    LogLookingGlas.ScrollTo(location.Line, location.Column);
                }
            }
        });
    }

    private void SelectWholeLine()
    {
        Dispatcher.Invoke(() =>
        {
            // Check which editor's context menu is active
            if (_isLookingGlassContextMenuActive && LogLookingGlas.Document != null)
            {
                var line = LogLookingGlas.Document.GetLineByOffset(LogLookingGlas.CaretOffset);
                LogLookingGlas.Select(line.Offset, line.Length);
                LogLookingGlas.ScrollTo(line.LineNumber, 0);
            }
            else if (LogEditor.Document != null)
            {
                var line = LogEditor.Document.GetLineByOffset(LogEditor.CaretOffset);
                LogEditor.Select(line.Offset, line.Length);
                LogEditor.ScrollTo(line.LineNumber, 0);
            }
        });
    }

    private void SelectAll()
    {
        Dispatcher.Invoke(() =>
        {
            // Check which editor's context menu is active
            if (_isLookingGlassContextMenuActive)
            {
                LogLookingGlas.SelectAll();
            }
            else
            {
                LogEditor.SelectAll();
            }
        });
    }

    private void ScrollToLine(string lineContent, string selectedText)
    {
        if (LogEditor.Document == null || string.IsNullOrEmpty(lineContent))
            return;

        Dispatcher.Invoke(() =>
        {
            // Find the line in the current document
            var text = LogEditor.Text;
            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim() == lineContent.Trim())
                {
                    // Found the line, scroll to it
                    var lineNumber = i + 1;
                    if (lineNumber <= LogEditor.Document.LineCount)
                    {
                        var documentLine = LogEditor.Document.GetLineByNumber(lineNumber);
                        LogEditor.ScrollTo(lineNumber, 0);
                        LogEditor.CaretOffset = documentLine.Offset;

                        // Try to restore the selection if there was any
                        if (!string.IsNullOrEmpty(selectedText))
                        {
                            var lineText = LogEditor.Document.GetText(documentLine.Offset, documentLine.Length);
                            var selectionIndex = lineText.IndexOf(selectedText, StringComparison.Ordinal);

                            if (selectionIndex >= 0)
                            {
                                // Found the selected text within the line, restore the selection
                                var selectionStart = documentLine.Offset + selectionIndex;
                                LogEditor.Select(selectionStart, selectedText.Length);

                                // Scroll to make the selection visible
                                var location = LogEditor.Document.GetLocation(selectionStart);
                                LogEditor.ScrollTo(location.Line, location.Column);
                            }
                        }

                        // Update the status
                        _viewModel.StatusText += $" (Restored to line {lineNumber})";
                    }
                    return;
                }
            }

            // If exact line not found, try to find the closest match or just go to the top
            if (LogEditor.Document.LineCount > 0)
            {
                LogEditor.ScrollToHome();
            }
        });
    }

    private void UpdateSearchHighlighting()
    {
        _searchHighlighter.SearchTerm = _viewModel.SearchText;
        _searchHighlighter.FindAllResults(LogEditor.Document);
        _viewModel.TotalSearchResults = _searchHighlighter.SearchResults.Count;
        _viewModel.CurrentSearchResultIndex = _searchHighlighter.SearchResults.Count > 0 ? 0 : -1;

        // Redraw efficiently
        LogEditor.TextArea.TextView.Redraw();

        // Navigate to first result
        if (_searchHighlighter.SearchResults.Count > 0)
        {
            NavigateToSearchResult(0);
        }
    }

    private void NavigateToSearchResult(int index)
    {
        if (_searchHighlighter.SearchResults.Count == 0 || index < 0 || index >= _searchHighlighter.SearchResults.Count)
            return;

        Dispatcher.Invoke(() =>
        {
            var result = _searchHighlighter.SearchResults[index];
            _searchHighlighter.CurrentResultIndex = index;

            // Select the text
            LogEditor.Select(result.StartOffset, result.Length);

            // Scroll to make it visible
            var location = LogEditor.Document.GetLocation(result.StartOffset);
            LogEditor.ScrollTo(location.Line, location.Column);

            // Redraw only affected area
            LogEditor.TextArea.TextView.Redraw();

            // Update status
            _viewModel.StatusText = $"Result {index + 1} of {_searchHighlighter.SearchResults.Count}";
        });
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        WpfApp.Current.Shutdown();
    }

    private void AutoUpdateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Save the setting when checkbox is toggled
        _ = _viewModel.SaveAutoUpdateSettingAsync();
    }

    private void FilterSearchCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Save the setting and apply/remove filter
        _ = _viewModel.SaveFilterSearchSettingAsync();
        _viewModel.ApplySearchFilter();
    }

    private void LogLookingGlas_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
    {
        // Set flag to indicate LogLookingGlas context menu is active
        _isLookingGlassContextMenuActive = true;

        // Update the ViewModel's SelectedText with the currently selected text in LogLookingGlas
        // This ensures commands in the context menu work with the correct text
        if (LogLookingGlas != null)
        {
            _viewModel.SelectedText = LogLookingGlas.SelectedText;

            // Also update the current line
            if (LogLookingGlas.Document != null && LogLookingGlas.CaretOffset <= LogLookingGlas.Document.TextLength)
            {
                var line = LogLookingGlas.Document.GetLineByOffset(LogLookingGlas.CaretOffset);
                _viewModel.CurrentLine = LogLookingGlas.Document.GetText(line.Offset, line.Length);
            }
        }
    }

    private void LogEditor_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
    {
        // Set flag to indicate LogEditor context menu is active
        _isLookingGlassContextMenuActive = false;

        // Update the ViewModel's SelectedText with the currently selected text in LogEditor
        // This ensures commands in the context menu work with the correct text
        if (LogEditor != null)
        {
            _viewModel.SelectedText = LogEditor.SelectedText;

            // Also update the current line
            if (LogEditor.Document != null && LogEditor.CaretOffset <= LogEditor.Document.TextLength)
            {
                var line = LogEditor.Document.GetLineByOffset(LogEditor.CaretOffset);
                _viewModel.CurrentLine = LogEditor.Document.GetText(line.Offset, line.Length);
            }
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var serviceProvider = (WpfApp.Current as App)?.Services;
        if (serviceProvider != null)
        {
            var settingsWindow = serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            // Refresh cached settings after closing settings window
            _viewModel.RefreshSettingsCache();
        }
    }

    private void ShowLookingGlass_Click(object sender, RoutedEventArgs e)
    {
        // Save the setting when menu item is toggled
        _ = _viewModel.SaveShowLookingGlassSettingAsync();

        // Update the row definition to properly collapse/expand
        UpdateLookingGlassRowHeight();
    }

    private void UpdateLookingGlassRowHeight()
    {
        // Row 4 is the Looking Glass row
        if (_viewModel.ShowLookingGlass)
        {
            // When visible, share space with LogEditor
            LogLookingGlasGroupBox.Parent.GetType().GetProperty("RowDefinitions")?.GetValue(LogLookingGlasGroupBox.Parent);
            var grid = (Grid)LogLookingGlasGroupBox.Parent;
            grid.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            // When hidden, collapse the row
            var grid = (Grid)LogLookingGlasGroupBox.Parent;
            grid.RowDefinitions[4].Height = new GridLength(0);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _viewModel.OnSearchResultChanged -= NavigateToSearchResult;
        _viewModel.OnScrollToLine -= ScrollToLine;
        _viewModel.OnSelectWholeLineRequested -= SelectWholeLine;
        _viewModel.OnSelectAllRequested -= SelectAll;
        _viewModel.OnUpdateLookingGlassRequested -= UpdateLookingGlass;

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

// Simple RelayCommand for keyboard shortcuts
public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
