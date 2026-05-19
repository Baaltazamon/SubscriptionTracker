namespace SubscriptionTracker.Application.DTO;

public sealed class CancellationRecommendationDto
{
    public Guid SubscriptionId { get; init; }

    public string SubscriptionName { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public decimal MonthlyCostInBaseCurrency { get; init; }

    public string BaseCurrency { get; init; } = "RUB";

    public string ReasonLabel { get; init; } = string.Empty;

    public string MonthlyCostLabel => $"{MonthlyCostInBaseCurrency:N2} {BaseCurrency}";
}
