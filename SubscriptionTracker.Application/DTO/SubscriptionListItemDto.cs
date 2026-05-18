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

    public string NextPaymentLabel => NextPaymentDate.ToString("dd.MM.yyyy");

    public string StatusLabel => IsActive ? "Активна" : "Отключена";

    public string MonthlyCostLabel => $"{MonthlyCostInBaseCurrency:N2} {BaseCurrency}";

    public int DaysUntil => NextPaymentDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;

    public string CountdownLabel => !IsActive
        ? "Не учитывается"
        : DaysUntil switch
        {
            < 0 => $"Просрочено на {Math.Abs(DaysUntil)} дн.",
            0 => "Сегодня",
            1 => "Через 1 день",
            < 5 => $"Через {DaysUntil} дня",
            _ => $"Через {DaysUntil} дней"
        };

    public string StatusBadgeLabel => !IsActive
        ? "Отключена"
        : DaysUntil <= 3
            ? "Скоро"
            : "Активна";

    public string StatusColorHex => !IsActive
        ? "#475569"
        : DaysUntil <= 3
            ? "#EA580C"
            : "#0284C7";
}
