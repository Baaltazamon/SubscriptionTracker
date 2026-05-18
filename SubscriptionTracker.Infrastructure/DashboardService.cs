using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Domain.Services;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure;

public sealed class DashboardService(AppDbContext dbContext, IAppSettingsService settingsService) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = settingsService.GetSettings();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(static subscription => subscription.Category)
            .Where(static subscription => subscription.IsActive)
            .ToListAsync(cancellationToken);

        var monthlyCosts = subscriptions
            .Select(subscription => new
            {
                Subscription = subscription,
                MonthlyCost = CurrencyConverter.Convert(
                    RecurringPaymentCalculator.GetMonthlyCost(subscription.Amount, subscription.BillingCycle),
                    subscription.Currency,
                    settings.BaseCurrency),
                YearlyCost = CurrencyConverter.Convert(
                    RecurringPaymentCalculator.GetYearlyCost(subscription.Amount, subscription.BillingCycle),
                    subscription.Currency,
                    settings.BaseCurrency)
            })
            .ToArray();

        var monthlyTotal = monthlyCosts.Sum(static item => item.MonthlyCost);
        var yearlyTotal = monthlyCosts.Sum(static item => item.YearlyCost);

        var upcomingPayments = subscriptions
            .Select(subscription => new UpcomingPaymentDto
            {
                SubscriptionId = subscription.Id,
                SubscriptionName = subscription.Name,
                CategoryName = subscription.Category.Name,
                Amount = subscription.Amount,
                Currency = subscription.Currency,
                PaymentDate = subscription.NextPaymentDate,
                Status = PaymentStatus.Planned,
                AmountInBaseCurrency = CurrencyConverter.Convert(subscription.Amount, subscription.Currency, settings.BaseCurrency),
                BaseCurrency = settings.BaseCurrency
            })
            .OrderBy(static payment => payment.PaymentDate)
            .Take(6)
            .ToArray();

        var mostExpensive = monthlyCosts
            .OrderByDescending(static item => item.YearlyCost)
            .FirstOrDefault();

        var categoryRaw = subscriptions
            .GroupBy(subscription => new { subscription.Category.Name, Color = subscription.Category.ColorHex ?? "#94A3B8" })
            .Select(group => new
            {
                group.Key.Name,
                group.Key.Color,
                Total = group.Sum(item => CurrencyConverter.Convert(
                    RecurringPaymentCalculator.GetMonthlyCost(item.Amount, item.BillingCycle),
                    item.Currency,
                    settings.BaseCurrency))
            })
            .OrderByDescending(static item => item.Total)
            .ToArray();

        var categoryExpenses = categoryRaw
            .Select(item => new CategoryExpenseDto
            {
                CategoryName = item.Name,
                ColorHex = item.Color,
                AmountInBaseCurrency = Math.Round(item.Total, 2, MidpointRounding.AwayFromZero),
                SharePercent = monthlyTotal == 0m ? 0m : Math.Round(item.Total / monthlyTotal * 100m, 1, MidpointRounding.AwayFromZero),
                AmountLabel = $"{item.Total:N2} {settings.BaseCurrency}"
            })
            .ToArray();

        var potentialSavingsMonthly = monthlyCosts
            .OrderByDescending(static item => item.MonthlyCost)
            .Take(3)
            .Sum(static item => item.MonthlyCost);

        return new DashboardSummaryDto
        {
            ActiveSubscriptionsCount = subscriptions.Count,
            MonthlyTotal = Math.Round(monthlyTotal, 2, MidpointRounding.AwayFromZero),
            YearlyTotal = Math.Round(yearlyTotal, 2, MidpointRounding.AwayFromZero),
            NextPayment = upcomingPayments.FirstOrDefault(),
            MostExpensiveSubscriptionName = mostExpensive?.Subscription.Name ?? LocalizationCatalog.Get("MostExpensiveFallback"),
            DailySpend = Math.Round(monthlyTotal / 30m, 2, MidpointRounding.AwayFromZero),
            PotentialSavingsMonthly = Math.Round(potentialSavingsMonthly, 2, MidpointRounding.AwayFromZero),
            BaseCurrency = settings.BaseCurrency,
            UpcomingPayments = upcomingPayments,
            CategoryExpenses = categoryExpenses,
            MonthlyForecast = BuildForecast(subscriptions, today, settings.BaseCurrency)
        };
    }

    private static IReadOnlyList<MonthlyForecastPointDto> BuildForecast(
        IReadOnlyCollection<Domain.Entities.Subscription> subscriptions,
        DateOnly today,
        string baseCurrency)
    {
        var result = new List<MonthlyForecastPointDto>(12);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        for (var offset = 0; offset < 12; offset++)
        {
            var start = monthStart.AddMonths(offset);
            var end = start.AddMonths(1).AddDays(-1);
            decimal total = 0m;

            foreach (var subscription in subscriptions)
            {
                var occurrence = subscription.NextPaymentDate;
                while (occurrence <= end)
                {
                    if (occurrence >= start)
                    {
                        total += CurrencyConverter.Convert(subscription.Amount, subscription.Currency, baseCurrency);
                    }

                    occurrence = RecurringPaymentCalculator.GetNextDate(occurrence, subscription.BillingCycle);
                }
            }

            result.Add(new MonthlyForecastPointDto
            {
                MonthLabel = start.ToString("MMM yy"),
                AmountInBaseCurrency = Math.Round(total, 2, MidpointRounding.AwayFromZero),
                AmountLabel = $"{total:N2} {baseCurrency}"
            });
        }

        return result;
    }
}
