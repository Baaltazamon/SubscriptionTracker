using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Domain.Services;

public static class RecurringPaymentCalculator
{
    public static DateOnly GetNextDate(DateOnly currentDate, BillingCycle billingCycle)
    {
        return billingCycle switch
        {
            BillingCycle.Monthly => currentDate.AddMonths(1),
            BillingCycle.Quarterly => currentDate.AddMonths(3),
            BillingCycle.SemiAnnual => currentDate.AddMonths(6),
            BillingCycle.Yearly => currentDate.AddYears(1),
            _ => currentDate.AddMonths(1)
        };
    }

    public static decimal GetMonthlyCost(decimal amount, BillingCycle billingCycle)
    {
        return billingCycle switch
        {
            BillingCycle.Monthly => amount,
            BillingCycle.Quarterly => amount / 3m,
            BillingCycle.SemiAnnual => amount / 6m,
            BillingCycle.Yearly => amount / 12m,
            _ => amount
        };
    }

    public static decimal GetYearlyCost(decimal amount, BillingCycle billingCycle)
    {
        return billingCycle switch
        {
            BillingCycle.Monthly => amount * 12m,
            BillingCycle.Quarterly => amount * 4m,
            BillingCycle.SemiAnnual => amount * 2m,
            BillingCycle.Yearly => amount,
            _ => amount * 12m
        };
    }

    public static DateOnly NormalizeFutureDate(DateOnly firstPaymentDate, DateOnly candidateDate, BillingCycle billingCycle, DateOnly today)
    {
        var normalized = candidateDate < firstPaymentDate ? firstPaymentDate : candidateDate;

        while (normalized < today)
        {
            normalized = GetNextDate(normalized, billingCycle);
        }

        return normalized;
    }
}
