using System.Globalization;
using System.Windows.Data;

namespace SubscriptionTracker.Wpf.Converters;

public sealed class PercentageToScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return Math.Clamp((double)(decimalValue / 100m), 0d, 1d);
        }

        if (value is double doubleValue)
        {
            return Math.Clamp(doubleValue / 100d, 0d, 1d);
        }

        return 0d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
