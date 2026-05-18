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

    public string PaymentDateLabel => PaymentDate.ToString("dd.MM.yyyy");

    public string AmountInBaseCurrencyLabel => $"{AmountInBaseCurrency:N2} {BaseCurrency}";

    public string StatusLabel => BillingCycleDisplayFormatter.ToLabel(Status);

    public int DaysUntil => PaymentDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;

    public string CountdownLabel => DaysUntil switch
    {
        < 0 => $"Просрочено на {Math.Abs(DaysUntil)} дн.",
        0 => "Сегодня",
        1 => "Через 1 день",
        < 5 => $"Через {DaysUntil} дня",
        _ => $"Через {DaysUntil} дней"
    };

    public string UrgencyLabel => DaysUntil switch
    {
        < 0 => "Просрочен",
        0 => "Сегодня",
        <= 3 => "Скоро",
        <= 7 => "На неделе",
        _ => "Запланирован"
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
