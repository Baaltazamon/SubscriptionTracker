namespace SubscriptionTracker.Application.DTO;

public sealed class MonthlyForecastPointDto
{
    public string MonthLabel { get; init; } = string.Empty;

    public decimal AmountInBaseCurrency { get; init; }

    public string AmountLabel { get; init; } = string.Empty;
}
