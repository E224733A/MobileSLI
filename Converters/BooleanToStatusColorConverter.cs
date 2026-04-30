using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Globalization;

namespace TourneesMobile.Converters;

public sealed class BooleanToStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ok = value is bool b && b;
        return ok
            ? Application.Current?.Resources["Success"] ?? Colors.Green
            : Application.Current?.Resources["TextMuted"] ?? Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StatutToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var statut = value?.ToString() ?? string.Empty;

        return statut switch
        {
            "FAIT" => Application.Current?.Resources["Success"] ?? Colors.Green,
            "NON_FAIT" => Application.Current?.Resources["Warning"] ?? Colors.Orange,
            "ANOMALIE" => Application.Current?.Resources["Danger"] ?? Colors.Red,
            _ => Application.Current?.Resources["TextMuted"] ?? Colors.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
