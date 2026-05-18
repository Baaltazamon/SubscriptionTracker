using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Application.DTO;

public sealed class PaymentHistoryDto
{
    public Guid Id { get; init; }

    public Guid SubscriptionId { get; init; }

    public string SubscriptionName { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "RUB";

    public DateOnly PaymentDate { get; init; }

    public PaymentStatus Status { get; init; }

    public string Note { get; init; } = string.Empty;

    public string AmountLabel { get; init; } = string.Empty;

    public string PaymentDateLabel { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public string NoteLabel => string.IsNullOrWhiteSpace(Note) ? LocalizationCatalog.Get("NoComment") : Note;

    public string StatusColorHex => Status switch
    {
        PaymentStatus.Paid => "#16A34A",
        PaymentStatus.Planned => "#475569",
        PaymentStatus.Skipped => "#EA580C",
        PaymentStatus.Cancelled => "#64748B",
        PaymentStatus.Failed => "#DC2626",
        _ => "#475569"
    };
}
