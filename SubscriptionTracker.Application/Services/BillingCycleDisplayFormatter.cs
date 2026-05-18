using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Application.Services;

public static class BillingCycleDisplayFormatter
{
    public static string ToLabel(BillingCycle cycle)
    {
        return cycle switch
        {
            BillingCycle.Monthly => "Каждый месяц",
            BillingCycle.Quarterly => "Каждый квартал",
            BillingCycle.SemiAnnual => "Раз в полгода",
            BillingCycle.Yearly => "Раз в год",
            _ => "Неизвестно"
        };
    }

    public static string ToLabel(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Planned => "Запланирован",
            PaymentStatus.Paid => "Оплачен",
            PaymentStatus.Skipped => "Пропущен",
            PaymentStatus.Cancelled => "Отменен",
            PaymentStatus.Failed => "Ошибка",
            _ => "Неизвестно"
        };
    }
}
