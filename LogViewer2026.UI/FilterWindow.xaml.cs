using System.Windows;
using System.Windows.Controls;
using LogViewer2026.UI.ViewModels;

namespace LogViewer2026.UI;

public partial class FilterWindow : Window
{
    private readonly MainViewModel _viewModel;

    public FilterWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
    }

    private void FilterStartTimeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel == null || sender is not System.Windows.Controls.TextBox textBox)
            return;

        UpdateFilterDateTime(isStartTime: true, textBox.Text);
    }

    private void FilterEndTimeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel == null || sender is not System.Windows.Controls.TextBox textBox)
            return;

        UpdateFilterDateTime(isStartTime: false, textBox.Text);
    }

    private void FilterStartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || FilterStartTimeBox == null)
            return;

        UpdateFilterDateTime(isStartTime: true, FilterStartTimeBox.Text);
    }

    private void FilterEndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || FilterEndTimeBox == null)
            return;

        UpdateFilterDateTime(isStartTime: false, FilterEndTimeBox.Text);
    }

    private void UpdateFilterDateTime(bool isStartTime, string timeText)
    {
        if (_viewModel == null || string.IsNullOrWhiteSpace(timeText))
            return;

        try
        {
            var currentDateTime = isStartTime ? _viewModel.FilterStartTime : _viewModel.FilterEndTime;
            var date = currentDateTime?.Date ?? DateTime.Today;

            if (TimeSpan.TryParse(timeText, out var time))
            {
                var newDateTime = date.Add(time);

                if (isStartTime)
                    _viewModel.FilterStartTime = newDateTime;
                else
                    _viewModel.FilterEndTime = newDateTime;
            }
        }
        catch
        {
            // Ignore invalid time input
        }
    }

    private void FilterSearchCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _ = _viewModel.SaveFilterSearchSettingAsync();
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        // Filters are already applied in real-time via bindings
        // This button just confirms and closes the window
        DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
