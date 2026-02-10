using System.ComponentModel;
using System.Windows;

namespace LogViewer2026.UI;

public partial class FloatingPanelWindow : Window
{
    public event Action? DockRequested;
    private bool _isForceClosing;

    public FloatingPanelWindow()
    {
        InitializeComponent();
    }

    public void SetContent(UIElement content)
    {
        ContentHost.Content = content;
    }

    public UIElement? RemoveContent()
    {
        var content = ContentHost.Content as UIElement;
        ContentHost.Content = null;
        return content;
    }

    public void ForceClose()
    {
        _isForceClosing = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isForceClosing && ContentHost.Content != null)
        {
            e.Cancel = true;
            DockRequested?.Invoke();
            return;
        }
        base.OnClosing(e);
    }
}
