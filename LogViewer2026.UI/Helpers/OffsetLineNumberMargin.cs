using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace LogViewer2026.UI.Helpers;

/// <summary>
/// Custom line number margin that supports an offset for displaying original line numbers
/// </summary>
public class OffsetLineNumberMargin : AbstractMargin
{
    private int _lineNumberOffset = 0;

    public int LineNumberOffset
    {
        get => _lineNumberOffset;
        set
        {
            if (_lineNumberOffset != value)
            {
                _lineNumberOffset = value;
                InvalidateVisual();
            }
        }
    }

    protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
    {
        var typeface = new Typeface(new System.Windows.Media.FontFamily("Consolas"), 
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        var textToMeasure = new FormattedText(
            new string('9', GetMaxLineNumberDigits()),
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            12,
            System.Windows.Media.Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        return new System.Windows.Size(textToMeasure.Width + 8, 0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var textView = TextView;
        if (textView?.Document == null || textView.VisualLinesValid == false)
            return;

        var renderSize = RenderSize;
        drawingContext.DrawRectangle(System.Windows.Media.Brushes.WhiteSmoke, null, 
            new Rect(0, 0, renderSize.Width, renderSize.Height));

        var typeface = new Typeface(new System.Windows.Media.FontFamily("Consolas"), 
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        foreach (var line in textView.VisualLines)
        {
            var lineNumber = line.FirstDocumentLine.LineNumber + _lineNumberOffset;
            var text = new FormattedText(
                lineNumber.ToString(),
                CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface,
                12,
                System.Windows.Media.Brushes.Gray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var y = line.VisualTop - textView.VerticalOffset;
            drawingContext.DrawText(text, new System.Windows.Point(renderSize.Width - text.Width - 4, y));
        }
    }

    private int GetMaxLineNumberDigits()
    {
        var textView = TextView;
        if (textView?.Document == null)
            return 3;

        var maxLineNumber = textView.Document.LineCount + _lineNumberOffset;
        return Math.Max(3, maxLineNumber.ToString().Length);
    }

    protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
    {
        if (oldTextView != null)
        {
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
        }

        base.OnTextViewChanged(oldTextView, newTextView);

        if (newTextView != null)
        {
            newTextView.VisualLinesChanged += OnVisualLinesChanged;
        }

        InvalidateMeasure();
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }
}
