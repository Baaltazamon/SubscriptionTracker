namespace SubscriptionTracker.Domain.Services;

public static class CurrencyConverter
{
    private static readonly IReadOnlyDictionary<string, decimal> DefaultRatesToRub = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["RUB"] = 1m,
        ["USD"] = 92m,
        ["EUR"] = 100m,
        ["GBP"] = 117m
    };

    public static decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        return Convert(amount, fromCurrency, toCurrency, null);
    }

    public static decimal Convert(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        IReadOnlyDictionary<string, decimal>? ratesToRub)
    {
        if (amount == 0m)
        {
            return 0m;
        }

        var normalizedRates = NormalizeRatesToRub(ratesToRub);
        var fromRate = GetRate(fromCurrency, normalizedRates);
        var toRate = GetRate(toCurrency, normalizedRates);
        var amountInRub = amount * fromRate;

        return Math.Round(amountInRub / toRate, 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyCollection<string> GetSupportedCurrencies()
    {
        return GetSupportedCurrencies(null);
    }

    public static IReadOnlyCollection<string> GetSupportedCurrencies(IReadOnlyDictionary<string, decimal>? ratesToRub)
    {
        return NormalizeRatesToRub(ratesToRub).Keys
            .OrderBy(static key => key)
            .ToArray();
    }

    public static IReadOnlyDictionary<string, decimal> GetDefaultRatesToRub()
    {
        return new Dictionary<string, decimal>(DefaultRatesToRub, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, decimal> NormalizeRatesToRub(IReadOnlyDictionary<string, decimal>? ratesToRub)
    {
        var normalized = new Dictionary<string, decimal>(DefaultRatesToRub, StringComparer.OrdinalIgnoreCase);

        if (ratesToRub is not null)
        {
            foreach (var pair in ratesToRub)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                var currency = pair.Key.Trim().ToUpperInvariant();
                if (pair.Value > 0m)
                {
                    normalized[currency] = pair.Value;
                }
            }
        }

        normalized["RUB"] = 1m;
        return normalized;
    }

    private static decimal GetRate(string currency, IReadOnlyDictionary<string, decimal> ratesToRub)
    {
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? "RUB"
            : currency.Trim().ToUpperInvariant();

        return ratesToRub.TryGetValue(normalizedCurrency, out var rate) ? rate : 1m;
    }
}
