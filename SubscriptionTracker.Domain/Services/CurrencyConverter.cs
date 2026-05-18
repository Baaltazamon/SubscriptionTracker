namespace SubscriptionTracker.Domain.Services;

public static class CurrencyConverter
{
    private static readonly IReadOnlyDictionary<string, decimal> RatesToRub = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["RUB"] = 1m,
        ["USD"] = 92m,
        ["EUR"] = 100m,
        ["GBP"] = 117m
    };

    public static decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        if (amount == 0m)
        {
            return 0m;
        }

        var fromRate = GetRate(fromCurrency);
        var toRate = GetRate(toCurrency);
        var amountInRub = amount * fromRate;

        return Math.Round(amountInRub / toRate, 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyCollection<string> GetSupportedCurrencies()
    {
        return RatesToRub.Keys.OrderBy(static key => key).ToArray();
    }

    private static decimal GetRate(string currency)
    {
        return RatesToRub.TryGetValue(currency, out var rate) ? rate : 1m;
    }
}
