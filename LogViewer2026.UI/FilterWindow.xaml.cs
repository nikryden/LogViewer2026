using System.Windows;
using System.Windows.Controls;
using LogViewer2026.UI.ViewModels;

namespace LogViewer2026.UI;

public partial class FilterWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _isInitializing;

    public FilterWindow(MainViewModel viewModel)
    {
        _isInitializing = true;
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        // Sync time pickers to any pre-existing filter times so that saved filters
        // with a time component are displayed and preserved correctly.
        FilterStartTimePicker.SelectedTime = _viewModel.FilterStartTime.HasValue
            ? _viewModel.FilterStartTime.Value.TimeOfDay
            : TimeSpan.Zero;
        FilterEndTimePicker.SelectedTime = _viewModel.FilterEndTime.HasValue
            ? _viewModel.FilterEndTime.Value.TimeOfDay
            : new TimeSpan(23, 59, 0);

        _isInitializing = false;
    }

    private void FilterStartTimePicker_SelectedTimeChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        UpdateFilterDateTime(isStartTime: true, FilterStartDatePicker.SelectedDate, FilterStartTimePicker.SelectedTime);
    }

    private void FilterEndTimePicker_SelectedTimeChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        UpdateFilterDateTime(isStartTime: false, FilterEndDatePicker.SelectedDate, FilterEndTimePicker.SelectedTime);
    }

    private void FilterStartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null)
            return;

        var picker = (DatePicker)sender;
        UpdateFilterDateTime(isStartTime: true, picker.SelectedDate, FilterStartTimePicker.SelectedTime);
    }

    private void FilterEndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null)
            return;

        var picker = (DatePicker)sender;
        UpdateFilterDateTime(isStartTime: false, picker.SelectedDate, FilterEndTimePicker.SelectedTime);
    }

    private void UpdateFilterDateTime(bool isStartTime, DateTime? pickedDate, TimeSpan time)
    {
        if (_viewModel == null || _isInitializing)
            return;

        if (pickedDate == null)
        {
            // No date selected in the picker — clear the date/time filter
            if (isStartTime)
                _viewModel.FilterStartTime = null;
            else
                _viewModel.FilterEndTime = null;
            return;
        }

        var date = pickedDate.Value.Date;
        var newDateTime = date.Add(time);

        if (isStartTime)
            _viewModel.FilterStartTime = newDateTime;
        else
            _viewModel.FilterEndTime = newDateTime;
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
        // Apply filters when button is clicked using the command
        if (_viewModel?.ApplyFilterCommand?.CanExecute(null) == true)
        {
            _viewModel.ApplyFilterCommand.Execute(null);
        }
        DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ClearStartDateTime_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.FilterStartTime = null;
            FilterStartTimePicker.SelectedTime = TimeSpan.Zero;
        }
    }

    private void ClearEndDateTime_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.FilterEndTime = null;
            FilterEndTimePicker.SelectedTime = new TimeSpan(23, 59, 0);
        }
    }
}
