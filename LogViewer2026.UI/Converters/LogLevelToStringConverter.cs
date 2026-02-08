using System.Globalization;
using System.Windows.Data;

namespace LogViewer2026.UI.Converters;

public sealed class LogLevelToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return "All";

        return value.ToString() ?? "All";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            if (str == "All")
                return null;

            if (Enum.TryParse<Core.Models.LogLevel>(str, out var level))
                return level;
        }

        return null;
    }
}
