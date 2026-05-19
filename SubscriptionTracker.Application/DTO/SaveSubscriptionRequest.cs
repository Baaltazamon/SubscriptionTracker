using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Application.DTO;

public sealed class SaveSubscriptionRequest
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid CategoryId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "RUB";

    public BillingCycle BillingCycle { get; init; }

    public DateOnly FirstPaymentDate { get; init; }

    public DateOnly NextPaymentDate { get; init; }

    public bool IsActive { get; init; } = true;

    public bool AutoRenewal { get; init; } = true;

    public int ReminderDaysBefore { get; init; } = 3;

    public bool IsLowUsage { get; init; }
}
