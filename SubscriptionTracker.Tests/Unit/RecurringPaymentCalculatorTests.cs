using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Domain.Services;

namespace SubscriptionTracker.Tests.Unit;

public sealed class RecurringPaymentCalculatorTests
{
    [Fact]
    public void GetNextDate_ForMonthlyCycle_ReturnsNextMonth()
    {
        var currentDate = new DateOnly(2026, 5, 17);

        var result = RecurringPaymentCalculator.GetNextDate(currentDate, BillingCycle.Monthly);

        Assert.Equal(new DateOnly(2026, 6, 17), result);
    }

    [Theory]
    [InlineData(BillingCycle.Monthly, 100, 100)]
    [InlineData(BillingCycle.Quarterly, 300, 100)]
    [InlineData(BillingCycle.SemiAnnual, 600, 100)]
    [InlineData(BillingCycle.Yearly, 1200, 100)]
    public void GetMonthlyCost_ConvertsToMonthlyEquivalent(BillingCycle cycle, decimal amount, decimal expected)
    {
        var result = RecurringPaymentCalculator.GetMonthlyCost(amount, cycle);

        Assert.Equal(expected, result);
    }
}
