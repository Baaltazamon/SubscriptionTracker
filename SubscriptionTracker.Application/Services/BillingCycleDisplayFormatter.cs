using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Application.Services;

public static class BillingCycleDisplayFormatter
{
    public static string ToLabel(BillingCycle cycle)
    {
        return cycle switch
        {
            BillingCycle.Monthly => LocalizationCatalog.Get("BillingCycleMonthly"),
            BillingCycle.Quarterly => LocalizationCatalog.Get("BillingCycleQuarterly"),
            BillingCycle.SemiAnnual => LocalizationCatalog.Get("BillingCycleSemiAnnual"),
            BillingCycle.Yearly => LocalizationCatalog.Get("BillingCycleYearly"),
            _ => LocalizationCatalog.Get("Unknown")
        };
    }

    public static string ToLabel(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Planned => LocalizationCatalog.Get("PaymentStatusPlanned"),
            PaymentStatus.Paid => LocalizationCatalog.Get("PaymentStatusPaid"),
            PaymentStatus.Skipped => LocalizationCatalog.Get("PaymentStatusSkipped"),
            PaymentStatus.Cancelled => LocalizationCatalog.Get("PaymentStatusCancelled"),
            PaymentStatus.Failed => LocalizationCatalog.Get("PaymentStatusFailed"),
            _ => LocalizationCatalog.Get("Unknown")
        };
    }
}
