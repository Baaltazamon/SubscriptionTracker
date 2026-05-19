namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class AnalyticsTopSubscriptionItem
{
    public int Rank { get; init; }

    public string Name { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string MonthlyCostLabel { get; init; } = string.Empty;

    public string ShareLabel { get; init; } = string.Empty;
}
