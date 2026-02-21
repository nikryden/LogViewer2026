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
    private readonly SolidColorBrush _highlightBrush = new(MediaColor.FromArgb(150, 255, 255, 0)); // Yellow
    private readonly SolidColorBrush _currentBrush = new(MediaColor.FromArgb(200, 255, 165, 0)); // Orange
    private int _currentResultIndex = -1;
    private List<TextSegment> _searchResults = new();

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            _searchTerm = value;
            _currentResultIndex = -1;
            _searchResults.Clear();
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
        var index = 0;

        while (index < searchText.Length)
        {
            index = searchText.IndexOf(_searchTerm, index, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;

            _searchResults.Add(new TextSegment { StartOffset = index, Length = _searchTerm.Length });
            index += _searchTerm.Length;
        }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (string.IsNullOrEmpty(_searchTerm))
            return;

        var lineText = CurrentContext.Document.GetText(line);
        var index = 0;

        while (index < lineText.Length)
        {
            index = lineText.IndexOf(_searchTerm, index, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;

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

public class TextSegment
{
    public int StartOffset { get; set; }
    public int Length { get; set; }
}
