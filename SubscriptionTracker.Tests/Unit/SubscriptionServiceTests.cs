using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Domain.Services;
using SubscriptionTracker.Infrastructure;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Tests.Unit;

public sealed class SubscriptionServiceTests
{
    private static readonly Guid StreamingCategoryId = Guid.Parse("8D9265A8-DC96-42C6-BEB6-70E57CD43545");

    [Fact]
    public async Task SaveAsync_NewSubscription_CreatesPlannedPayment_AndPersistsLowUsage()
    {
        using var database = new SqliteTestDbContextFactory();
        await SeedCategoryAsync(database);

        await using var context = database.CreateContext();
        var service = CreateService(context);
        var nextPaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var result = await service.SaveAsync(new SaveSubscriptionRequest
        {
            Name = "Notion Plus",
            Description = "Notes and docs",
            CategoryId = StreamingCategoryId,
            Amount = 499m,
            Currency = "RUB",
            BillingCycle = BillingCycle.Monthly,
            FirstPaymentDate = nextPaymentDate,
            NextPaymentDate = nextPaymentDate,
            IsActive = true,
            AutoRenewal = true,
            ReminderDaysBefore = 2,
            IsLowUsage = true
        });

        Assert.True(result.IsLowUsage);

        await using var verification = database.CreateContext();
        var subscription = await verification.Subscriptions.Include(static item => item.Payments).SingleAsync();
        Assert.True(subscription.IsLowUsage);
        Assert.Equal(nextPaymentDate, subscription.NextPaymentDate);

        var plannedPayment = Assert.Single(subscription.Payments);
        Assert.Equal(PaymentStatus.Planned, plannedPayment.Status);
        Assert.Equal(nextPaymentDate, plannedPayment.PaymentDate);
    }

    [Fact]
    public async Task MarkAsPaidAsync_MarksCurrentPaymentPaid_AndSchedulesNextPayment()
    {
        using var database = new SqliteTestDbContextFactory();
        var nextPaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var subscriptionId = await SeedActiveSubscriptionAsync(database, nextPaymentDate);

        await using var context = database.CreateContext();
        var service = CreateService(context);

        await service.MarkAsPaidAsync(subscriptionId);

        await using var verification = database.CreateContext();
        var subscription = await verification.Subscriptions.Include(static item => item.Payments).SingleAsync();
        var payments = subscription.Payments.OrderBy(static item => item.PaymentDate).ToArray();

        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), subscription.LastUsedDate);
        Assert.Equal(RecurringPaymentCalculator.GetNextDate(nextPaymentDate, BillingCycle.Monthly), subscription.NextPaymentDate);
        Assert.Equal(2, payments.Length);
        Assert.Equal(PaymentStatus.Paid, payments[0].Status);
        Assert.Equal(PaymentStatus.Planned, payments[1].Status);
        Assert.Equal(RecurringPaymentCalculator.GetNextDate(nextPaymentDate, BillingCycle.Monthly), payments[1].PaymentDate);
    }

    [Fact]
    public async Task SkipNextPaymentAsync_MarksCurrentPaymentSkipped_AndSchedulesNextPayment()
    {
        using var database = new SqliteTestDbContextFactory();
        var nextPaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var subscriptionId = await SeedActiveSubscriptionAsync(database, nextPaymentDate);

        await using var context = database.CreateContext();
        var service = CreateService(context);

        await service.SkipNextPaymentAsync(subscriptionId);

        await using var verification = database.CreateContext();
        var subscription = await verification.Subscriptions.Include(static item => item.Payments).SingleAsync();
        var payments = subscription.Payments.OrderBy(static item => item.PaymentDate).ToArray();

        Assert.Equal(RecurringPaymentCalculator.GetNextDate(nextPaymentDate, BillingCycle.Monthly), subscription.NextPaymentDate);
        Assert.Equal(2, payments.Length);
        Assert.Equal(PaymentStatus.Skipped, payments[0].Status);
        Assert.Equal(PaymentStatus.Planned, payments[1].Status);
        Assert.Equal(RecurringPaymentCalculator.GetNextDate(nextPaymentDate, BillingCycle.Monthly), payments[1].PaymentDate);
    }

    [Fact]
    public async Task ToggleActiveAsync_DisableAndEnable_UpdatesPlannedPaymentsCorrectly()
    {
        using var database = new SqliteTestDbContextFactory();
        var nextPaymentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4));
        var subscriptionId = await SeedActiveSubscriptionAsync(database, nextPaymentDate);

        await using var disableContext = database.CreateContext();
        var disableService = CreateService(disableContext);
        await disableService.ToggleActiveAsync(subscriptionId);

        await using (var verification = database.CreateContext())
        {
            var subscription = await verification.Subscriptions.Include(static item => item.Payments).SingleAsync();
            Assert.False(subscription.IsActive);
            Assert.Single(subscription.Payments);
            Assert.Equal(PaymentStatus.Cancelled, subscription.Payments.Single().Status);
        }

        await using var enableContext = database.CreateContext();
        var enableService = CreateService(enableContext);
        await enableService.ToggleActiveAsync(subscriptionId);

        await using var finalVerification = database.CreateContext();
        var finalSubscription = await finalVerification.Subscriptions.Include(static item => item.Payments).SingleAsync();
        var payments = finalSubscription.Payments.OrderBy(static item => item.PaymentDate).ToArray();

        Assert.True(finalSubscription.IsActive);
        Assert.Equal(2, payments.Length);
        Assert.Equal(PaymentStatus.Cancelled, payments[0].Status);
        Assert.Equal(PaymentStatus.Planned, payments[1].Status);
        Assert.Equal(nextPaymentDate, payments[1].PaymentDate);
    }

    private static SubscriptionService CreateService(AppDbContext context)
    {
        return new SubscriptionService(context, new TestAppSettingsService(new AppSettingsDto
        {
            BaseCurrency = "RUB"
        }));
    }

    private static async Task SeedCategoryAsync(SqliteTestDbContextFactory database)
    {
        await using var context = database.CreateContext();
        context.Categories.Add(new Category
        {
            Id = StreamingCategoryId,
            Name = "Streaming",
            ColorHex = "#F97316",
            IsSystem = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task<Guid> SeedActiveSubscriptionAsync(SqliteTestDbContextFactory database, DateOnly nextPaymentDate)
    {
        await SeedCategoryAsync(database);

        var subscriptionId = Guid.NewGuid();
        await using var context = database.CreateContext();
        context.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            Name = "Spotify",
            CategoryId = StreamingCategoryId,
            Amount = 299m,
            Currency = "RUB",
            BillingCycle = BillingCycle.Monthly,
            FirstPaymentDate = nextPaymentDate,
            NextPaymentDate = nextPaymentDate,
            IsActive = true,
            AutoRenewal = true,
            ReminderDaysBefore = 2,
            CreatedAtUtc = DateTime.UtcNow,
            Payments =
            [
                new PaymentHistory
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscriptionId,
                    Amount = 299m,
                    Currency = "RUB",
                    PaymentDate = nextPaymentDate,
                    Status = PaymentStatus.Planned,
                    CreatedAtUtc = DateTime.UtcNow
                }
            ]
        });

        await context.SaveChangesAsync();
        return subscriptionId;
    }
}
