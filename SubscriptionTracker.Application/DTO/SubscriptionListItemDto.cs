using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Application.Services;
using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Application.DTO;

public sealed class SubscriptionListItemDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "RUB";

    public BillingCycle BillingCycle { get; init; }

    public DateOnly FirstPaymentDate { get; init; }

    public DateOnly NextPaymentDate { get; init; }

    public bool IsActive { get; init; }

    public bool AutoRenewal { get; init; }

    public int ReminderDaysBefore { get; init; }

    public decimal MonthlyCostInBaseCurrency { get; init; }

    public string BaseCurrency { get; init; } = "RUB";

    public bool DueSoon { get; init; }

    public string AmountLabel => $"{Amount:N2} {Currency}";

    public string CycleLabel => BillingCycleDisplayFormatter.ToLabel(BillingCycle);

    public string NextPaymentLabel => NextPaymentDate.ToString("d");

    public string StatusLabel => IsActive
        ? LocalizationCatalog.Get("StatusActive")
        : LocalizationCatalog.Get("StatusInactive");

    public string MonthlyCostLabel => $"{MonthlyCostInBaseCurrency:N2} {BaseCurrency}";

    public int DaysUntil => NextPaymentDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;

    public string CountdownLabel => !IsActive
        ? LocalizationCatalog.Get("NotCounted")
        : DaysUntil switch
        {
            < 0 => LocalizationCatalog.Format("CountdownOverdue", Math.Abs(DaysUntil)),
            0 => LocalizationCatalog.Get("CountdownToday"),
            1 => LocalizationCatalog.Get("CountdownInOneDay"),
            < 5 => LocalizationCatalog.Format("CountdownInFewDays", DaysUntil),
            _ => LocalizationCatalog.Format("CountdownInManyDays", DaysUntil)
        };

    public string StatusBadgeLabel => !IsActive
        ? LocalizationCatalog.Get("StatusInactive")
        : DaysUntil <= 3
            ? LocalizationCatalog.Get("UrgencySoon")
            : LocalizationCatalog.Get("StatusActive");

    public string StatusColorHex => !IsActive
        ? "#475569"
        : DaysUntil <= 3
            ? "#EA580C"
            : "#0284C7";
}
