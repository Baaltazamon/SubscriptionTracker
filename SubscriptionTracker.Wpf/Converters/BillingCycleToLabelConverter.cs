using System.Globalization;
using System.Windows.Data;
using SubscriptionTracker.Application.Services;
using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Wpf.Converters;

public sealed class BillingCycleToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is BillingCycle cycle
            ? BillingCycleDisplayFormatter.ToLabel(cycle)
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
