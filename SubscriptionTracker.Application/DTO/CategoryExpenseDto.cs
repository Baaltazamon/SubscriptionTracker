namespace SubscriptionTracker.Application.DTO;

public sealed class CategoryExpenseDto
{
    public string CategoryName { get; init; } = string.Empty;

    public string ColorHex { get; init; } = "#94A3B8";

    public decimal AmountInBaseCurrency { get; init; }

    public decimal SharePercent { get; init; }

    public string AmountLabel { get; init; } = string.Empty;

    public string ShareLabel => $"{SharePercent:0.#}%";
}
