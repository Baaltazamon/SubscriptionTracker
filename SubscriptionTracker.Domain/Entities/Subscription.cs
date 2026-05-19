using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "RUB";

    public BillingCycle BillingCycle { get; set; }

    public DateOnly FirstPaymentDate { get; set; }

    public DateOnly NextPaymentDate { get; set; }

    public bool IsActive { get; set; } = true;

    public bool AutoRenewal { get; set; } = true;

    public int ReminderDaysBefore { get; set; } = 3;

    public bool IsLowUsage { get; set; }

    public DateOnly? LastUsedDate { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<PaymentHistory> Payments { get; set; } = new List<PaymentHistory>();
}
