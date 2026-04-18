using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace LogViewer2026.UI.Controls;

public partial class TimePicker : UserControl
{
    private bool _suppressEvent;

    public static readonly DependencyProperty SelectedTimeProperty =
        DependencyProperty.Register(
            nameof(SelectedTime),
            typeof(TimeSpan),
            typeof(TimePicker),
            new FrameworkPropertyMetadata(TimeSpan.Zero,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedTimeChanged));

    public static readonly RoutedEvent SelectedTimeChangedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(SelectedTimeChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(TimePicker));

    public TimeSpan SelectedTime
    {
        get => (TimeSpan)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public event RoutedEventHandler SelectedTimeChanged
    {
        add => AddHandler(SelectedTimeChangedEvent, value);
        remove => RemoveHandler(SelectedTimeChangedEvent, value);
    }

    public TimePicker()
    {
        InitializeComponent();

        for (int h = 0; h < 24; h++)
            HourCombo.Items.Add(h.ToString("D2"));

        for (int m = 0; m < 60; m++)
            MinuteCombo.Items.Add(m.ToString("D2"));

        HourCombo.SelectedIndex = 0;
        MinuteCombo.SelectedIndex = 0;
    }

    private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (TimePicker)d;
        var ts = (TimeSpan)e.NewValue;
        picker._suppressEvent = true;
        picker.HourCombo.SelectedIndex = ts.Hours;
        picker.MinuteCombo.SelectedIndex = ts.Minutes;
        picker._suppressEvent = false;
    }

    private void OnTimeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvent || HourCombo.SelectedIndex < 0 || MinuteCombo.SelectedIndex < 0)
            return;

        SelectedTime = new TimeSpan(HourCombo.SelectedIndex, MinuteCombo.SelectedIndex, 0);
        RaiseEvent(new RoutedEventArgs(SelectedTimeChangedEvent, this));
    }
}
