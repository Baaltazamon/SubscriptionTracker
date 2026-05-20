using System.Globalization;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class CurrencyRateItemViewModel : ViewModelBase
{
    private string _rateText = string.Empty;

    public required string CurrencyCode { get; init; }

    public required string DisplayLabel { get; init; }

    public bool IsReadOnly => string.Equals(CurrencyCode, "RUB", StringComparison.OrdinalIgnoreCase);

    public string RateText
    {
        get => _rateText;
        set => SetProperty(ref _rateText, value);
    }

    public decimal? ParseRate()
    {
        if (decimal.TryParse(RateText, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureValue))
        {
            return currentCultureValue;
        }

        if (decimal.TryParse(RateText, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        return null;
    }
}
