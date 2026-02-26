using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;

namespace LogViewer2026.UI.Highlighting;

public sealed class SearchResultHighlighter : DocumentColorizingTransformer
{
    private string _searchTerm = string.Empty;
    private bool _useRegex = false;
    private Regex? _regexPattern;
    private string _cachedPattern = string.Empty;
    private bool _cachedUseRegex = false;
    private readonly SolidColorBrush _highlightBrush = new(MediaColor.FromArgb(150, 255, 255, 0)); // Yellow
    private readonly SolidColorBrush _currentBrush = new(MediaColor.FromArgb(200, 255, 165, 0)); // Orange
    private int _currentResultIndex = -1;
    private List<TextSegment> _searchResults = new();

    // Performance optimization: reuse options
    private static readonly RegexOptions _regexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant;
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (_searchTerm == value) return; // Skip if same

            _searchTerm = value;
            _currentResultIndex = -1;
            _searchResults.Clear();
            UpdateRegexPattern();
        }
    }

    public bool UseRegex
    {
        get => _useRegex;
        set
        {
            if (_useRegex == value) return; // Skip if same

            _useRegex = value;
            UpdateRegexPattern();
        }
    }

    private void UpdateRegexPattern()
    {
        // Check if we can reuse cached regex
        if (_cachedUseRegex == _useRegex && 
            _cachedPattern == _searchTerm && 
            _regexPattern != null)
        {
            return; // Already compiled
        }

        _regexPattern = null;
        _cachedPattern = _searchTerm;
        _cachedUseRegex = _useRegex;

        if (_useRegex && !string.IsNullOrEmpty(_searchTerm))
        {
            try
            {
                // Use compiled regex with optimizations
                _regexPattern = new Regex(_searchTerm, _regexOptions, _regexTimeout);
            }
            catch
            {
                // Invalid regex pattern, fall back to literal search
                _regexPattern = null;
            }
        }
    }

    public int CurrentResultIndex
    {
        get => _currentResultIndex;
        set => _currentResultIndex = value;
    }

    public List<TextSegment> SearchResults => _searchResults;

    public void FindAllResults(ITextSource text)
    {
        _searchResults.Clear();

        if (string.IsNullOrEmpty(_searchTerm) || text == null)
            return;

        var searchText = text.Text;

        // Pre-allocate capacity based on estimated matches (improves performance)
        var estimatedMatches = Math.Max(10, searchText.Length / 1000);
        if (_searchResults.Capacity < estimatedMatches)
            _searchResults.Capacity = estimatedMatches;

        if (_useRegex && _regexPattern != null)
        {
            // Use regex matching with optimized enumeration
            try
            {
                // Use ValueMatchEnumerator for better performance in .NET 10
                foreach (var match in _regexPattern.EnumerateMatches(searchText))
                {
                    _searchResults.Add(new TextSegment { StartOffset = match.Index, Length = match.Length });
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Regex timeout, return partial results
            }
        }
        else
        {
            // Use span-based literal string matching for better performance
            var searchSpan = searchText.AsSpan();
            var searchTermSpan = _searchTerm.AsSpan();
            var index = 0;

            while (index < searchSpan.Length)
            {
                var remainingSpan = searchSpan.Slice(index);
                var foundIndex = remainingSpan.IndexOf(searchTermSpan, StringComparison.OrdinalIgnoreCase);

                if (foundIndex < 0)
                    break;

                index += foundIndex;
                _searchResults.Add(new TextSegment { StartOffset = index, Length = _searchTerm.Length });
                index += _searchTerm.Length;
            }
        }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (string.IsNullOrEmpty(_searchTerm))
            return;

        var lineText = CurrentContext.Document.GetText(line);

        // Early exit if line is too short to contain search term
        if (lineText.Length < _searchTerm.Length)
            return;

        if (_useRegex && _regexPattern != null)
        {
            // Use regex matching with ValueMatchEnumerator
            try
            {
                foreach (var match in _regexPattern.EnumerateMatches(lineText))
                {
                    var startOffset = line.Offset + match.Index;
                    var endOffset = startOffset + match.Length;

                    // Check if this is the current result (optimized comparison)
                    var isCurrent = _currentResultIndex >= 0 &&
                                    _currentResultIndex < _searchResults.Count &&
                                    _searchResults[_currentResultIndex].StartOffset == startOffset;

                    ChangeLinePart(
                        startOffset,
                        endOffset,
                        element =>
                        {
                            element.TextRunProperties.SetBackgroundBrush(isCurrent ? _currentBrush : _highlightBrush);
                            if (isCurrent)
                            {
                                element.TextRunProperties.SetForegroundBrush(MediaBrushes.Black);
                            }
                        });
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Regex timeout, skip this line
            }
        }
        else
        {
            // Use span-based literal string matching for better performance
            var lineSpan = lineText.AsSpan();
            var searchSpan = _searchTerm.AsSpan();
            var index = 0;

            while (index < lineSpan.Length)
            {
                var remainingSpan = lineSpan.Slice(index);
                var foundIndex = remainingSpan.IndexOf(searchSpan, StringComparison.OrdinalIgnoreCase);

                if (foundIndex < 0)
                    break;

                index += foundIndex;
                var startOffset = line.Offset + index;
                var endOffset = startOffset + _searchTerm.Length;

                // Check if this is the current result
                var isCurrent = _currentResultIndex >= 0 &&
                                _currentResultIndex < _searchResults.Count &&
                                _searchResults[_currentResultIndex].StartOffset == startOffset;

                ChangeLinePart(
                    startOffset,
                    endOffset,
                    element =>
                    {
                        element.TextRunProperties.SetBackgroundBrush(isCurrent ? _currentBrush : _highlightBrush);
                        if (isCurrent)
                        {
                            element.TextRunProperties.SetForegroundBrush(MediaBrushes.Black);
                        }
                    });

                index += _searchTerm.Length;
            }
        }
    }
}

public class TextSegment
{
    public int StartOffset { get; set; }
    public int Length { get; set; }
}
