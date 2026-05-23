using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Application.DTO;

public sealed class ImportPreviewItemDto
{
    public int RowNumber { get; init; }

    public string Name { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "RUB";

    public BillingCycle BillingCycle { get; init; }

    public DateOnly NextPaymentDate { get; init; }

    public ImportPreviewAction Action { get; init; }

    public bool WillCreateCategory { get; init; }

    public string? Note { get; init; }
}
