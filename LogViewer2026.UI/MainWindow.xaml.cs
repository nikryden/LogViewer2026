using System.Windows;
using System.Windows.Input;
using LogViewer2026.UI.ViewModels;
using LogViewer2026.UI.Highlighting;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using WpfApp = System.Windows.Application;

namespace LogViewer2026.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SearchResultHighlighter _searchHighlighter;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Set up AvalonEdit
        LogEditor.SyntaxHighlighting = LogHighlighting.CreateLogHighlighting();

        // Set up search result highlighting
        _searchHighlighter = new SearchResultHighlighter();
        LogEditor.TextArea.TextView.LineTransformers.Add(_searchHighlighter);

        // Bind ViewModel properties to AvalonEdit
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Handle search result navigation
        _viewModel.OnSearchResultChanged += NavigateToSearchResult;

        // Handle editor actions
        _viewModel.OnSelectWholeLineRequested += SelectWholeLine;
        _viewModel.OnSelectAllRequested += SelectAll;

        // Handle text selection and update CurrentLine
        LogEditor.TextArea.SelectionChanged += (s, e) =>
        {
            _viewModel.SelectedText = LogEditor.SelectedText;
            UpdateCurrentLine();
        };

        // Update current line on caret position change
        LogEditor.TextArea.Caret.PositionChanged += (s, e) => UpdateCurrentLine();

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
                }
                else
                {
                    UpdateSearchHighlighting();
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

    private void SelectWholeLine()
    {
        Dispatcher.Invoke(() =>
        {
            if (LogEditor.Document == null)
                return;

            var line = LogEditor.Document.GetLineByOffset(LogEditor.CaretOffset);
            LogEditor.Select(line.Offset, line.Length);
            LogEditor.ScrollTo(line.LineNumber, 0);
        });
    }

    private void SelectAll()
    {
        Dispatcher.Invoke(() =>
        {
            LogEditor.SelectAll();
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

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var serviceProvider = (WpfApp.Current as App)?.Services;
        if (serviceProvider != null)
        {
            var settingsWindow = serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _viewModel.OnSearchResultChanged -= NavigateToSearchResult;
        _viewModel.OnSelectWholeLineRequested -= SelectWholeLine;
        _viewModel.OnSelectAllRequested -= SelectAll;

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

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
