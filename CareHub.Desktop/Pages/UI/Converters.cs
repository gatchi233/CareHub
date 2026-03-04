using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CareHub.Pages.UI.Converters
{
    public class ReminderTimeToColorConverter : IValueConverter
    {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not TimeSpan reminderTime)
                return Colors.Transparent;

            var now = DateTime.Now.TimeOfDay;
            var deltaMinutes = (reminderTime - now).TotalMinutes;

            int windowMinutes = 60;
            if (parameter is string s && int.TryParse(s, out var parsed))
                windowMinutes = parsed;

            if (deltaMinutes < 0)
                return Color.FromArgb("#FFE0E0E0");

            if (deltaMinutes <= 5)
                return Color.FromArgb("#FFFFCDD2");

            if (deltaMinutes <= windowMinutes)
                return Color.FromArgb("#FFFFF9C4");

            return Colors.White;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class IntToDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is int i ? (double)i : 0d;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is double d ? (int)Math.Round(d) : 0;
    }

    public class StringNotEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !string.IsNullOrWhiteSpace(value as string);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class IntPositiveConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is int i && i > 0;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToEnableDisableTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "Disable" : "Enable";

            return "Enable/Disable";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class InvertedBoolConverter : IValueConverter
    {
        // Converts True to False and False to True
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
                return !booleanValue;

            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
                return !booleanValue;

            return true;
        }
    }

    public class MarStatusToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var status = value as string ?? "";
            return status switch
            {
                "Given" => Application.Current?.Resources.TryGetValue("Badge_Given", out var g) == true ? g : Colors.Green,
                "Refused" => Application.Current?.Resources.TryGetValue("Badge_Refused", out var r) == true ? r : Colors.Red,
                "Held" => Application.Current?.Resources.TryGetValue("Badge_Held", out var h) == true ? h : Colors.Orange,
                "Missed" => Application.Current?.Resources.TryGetValue("Badge_Missed", out var m) == true ? m : Colors.Gray,
                "Pending" => Application.Current?.Resources.TryGetValue("Badge_Pending", out var p) == true ? p : Colors.LightGray,
                _ => Application.Current?.Resources.TryGetValue("Badge_NotAvailable", out var n) == true ? n : Colors.Gray,
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class UtcToLocalDateTimeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            var format = parameter as string ?? "yyyy-MM-dd HH:mm";

            if (value is DateTime dt)
            {
                var utc = dt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    : dt;
                return utc.ToLocalTime().ToString(format, culture);
            }

            if (value is DateTimeOffset dto)
            {
                return dto.ToLocalTime().ToString(format, culture);
            }

            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
