using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Application.Services;
using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Application.DTO;

public sealed class UpcomingPaymentDto
{
    public Guid SubscriptionId { get; init; }

    public string SubscriptionName { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "RUB";

    public DateOnly PaymentDate { get; init; }

    public PaymentStatus Status { get; init; }

    public decimal AmountInBaseCurrency { get; init; }

    public string BaseCurrency { get; init; } = "RUB";

    public string AmountLabel => $"{Amount:N2} {Currency}";

    public string PaymentDateLabel => PaymentDate.ToString("d");

    public string AmountInBaseCurrencyLabel => $"{AmountInBaseCurrency:N2} {BaseCurrency}";

    public string StatusLabel => BillingCycleDisplayFormatter.ToLabel(Status);

    public int DaysUntil => PaymentDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;

    public string CountdownLabel => DaysUntil switch
    {
        < 0 => LocalizationCatalog.Format("CountdownOverdue", Math.Abs(DaysUntil)),
        0 => LocalizationCatalog.Get("CountdownToday"),
        1 => LocalizationCatalog.Get("CountdownInOneDay"),
        < 5 => LocalizationCatalog.Format("CountdownInFewDays", DaysUntil),
        _ => LocalizationCatalog.Format("CountdownInManyDays", DaysUntil)
    };

    public string UrgencyLabel => DaysUntil switch
    {
        < 0 => LocalizationCatalog.Get("UrgencyOverdue"),
        0 => LocalizationCatalog.Get("UrgencyToday"),
        <= 3 => LocalizationCatalog.Get("UrgencySoon"),
        <= 7 => LocalizationCatalog.Get("UrgencyThisWeek"),
        _ => LocalizationCatalog.Get("UrgencyPlanned")
    };

    public string UrgencyColorHex => DaysUntil switch
    {
        < 0 => "#DC2626",
        0 => "#B91C1C",
        <= 3 => "#EA580C",
        <= 7 => "#0284C7",
        _ => "#475569"
    };
}
