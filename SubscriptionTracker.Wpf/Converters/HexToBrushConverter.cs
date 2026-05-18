using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SubscriptionTracker.Wpf.Converters;

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorHex && !string.IsNullOrWhiteSpace(colorHex))
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
