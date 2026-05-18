using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Domain.Entities;

public sealed class PaymentHistory
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "RUB";

    public DateOnly PaymentDate { get; set; }

    public PaymentStatus Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
