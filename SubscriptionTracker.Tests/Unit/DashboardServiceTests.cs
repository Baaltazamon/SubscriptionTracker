using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Infrastructure;

namespace SubscriptionTracker.Tests.Unit;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetAsync_UsesLowUsageSubscriptions_ForCancellationRecommendations()
    {
        using var database = new SqliteTestDbContextFactory();
        await SeedAnalyticsDataAsync(database);

        await using var context = database.CreateContext();
        var service = new DashboardService(context, new TestAppSettingsService(new AppSettingsDto
        {
            BaseCurrency = "RUB"
        }));

        var summary = await service.GetAsync();

        Assert.Equal(2, summary.CancellationRecommendations.Count);
        Assert.Equal("Adobe Creative Cloud", summary.CancellationRecommendations[0].SubscriptionName);
        Assert.Equal("Xbox Game Pass", summary.CancellationRecommendations[1].SubscriptionName);
        Assert.Equal(3889m, summary.PotentialSavingsMonthly);
    }

    [Fact]
    public async Task GetAsync_FallsBackToTopMonthlyCosts_WhenNoLowUsageFlagsExist()
    {
        using var database = new SqliteTestDbContextFactory();
        await SeedAnalyticsDataAsync(database, markLowUsage: false);

        await using var context = database.CreateContext();
        var service = new DashboardService(context, new TestAppSettingsService(new AppSettingsDto
        {
            BaseCurrency = "RUB"
        }));

        var summary = await service.GetAsync();

        Assert.Empty(summary.CancellationRecommendations);
        Assert.Equal(6389m, summary.PotentialSavingsMonthly);
    }

    private static async Task SeedAnalyticsDataAsync(SqliteTestDbContextFactory database, bool markLowUsage = true)
    {
        var categoryId = Guid.NewGuid();
        await using var context = database.CreateContext();

        context.Categories.Add(new Category
        {
            Id = categoryId,
            Name = "Software",
            ColorHex = "#0EA5E9",
            IsSystem = true
        });

        context.Subscriptions.AddRange(
            CreateSubscription("Adobe Creative Cloud", categoryId, 2990m, markLowUsage),
            CreateSubscription("Xbox Game Pass", categoryId, 899m, markLowUsage),
            CreateSubscription("ChatGPT Team", categoryId, 2500m, false));

        await context.SaveChangesAsync();
    }

    private static Subscription CreateSubscription(string name, Guid categoryId, decimal amount, bool isLowUsage)
    {
        var subscriptionId = Guid.NewGuid();
        var nextPaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        return new Subscription
        {
            Id = subscriptionId,
            Name = name,
            CategoryId = categoryId,
            Amount = amount,
            Currency = "RUB",
            BillingCycle = BillingCycle.Monthly,
            FirstPaymentDate = nextPaymentDate,
            NextPaymentDate = nextPaymentDate,
            IsActive = true,
            AutoRenewal = true,
            IsLowUsage = isLowUsage,
            ReminderDaysBefore = 3,
            CreatedAtUtc = DateTime.UtcNow,
            Payments =
            [
                new PaymentHistory
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscriptionId,
                    Amount = amount,
                    Currency = "RUB",
                    PaymentDate = nextPaymentDate,
                    Status = PaymentStatus.Planned,
                    CreatedAtUtc = DateTime.UtcNow
                }
            ]
        };
    }
}
