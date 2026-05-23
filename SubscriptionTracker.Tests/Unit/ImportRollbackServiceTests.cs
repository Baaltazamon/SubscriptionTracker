using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Infrastructure;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Tests.Unit;

public sealed class ImportRollbackServiceTests
{
    [Fact]
    public async Task RollbackLastImportAsync_RemovesCreatedSubscriptionsAndCategories()
    {
        using var database = new SqliteTestDbContextFactory();
        var csvPath = CreateTempFile(".csv");

        try
        {
            await File.WriteAllTextAsync(csvPath,
                "Name;Category;Amount;Currency;Cycle;Next payment" + Environment.NewLine +
                "Linear Pro;Work;15;USD;Monthly;2026-07-10");

            await using (var context = database.CreateContext())
            {
                var importService = CreateImportService(context);
                await importService.ImportAsync(csvPath);
            }

            await using (var verification = database.CreateContext())
            {
                Assert.Single(await verification.Subscriptions.ToListAsync());
                Assert.Contains(await verification.Categories.Select(static item => item.Name).ToListAsync(), static name => name == "Work");
            }

            await using (var rollbackContext = database.CreateContext())
            {
                var rollbackService = new ImportRollbackService(rollbackContext);
                var preview = await rollbackService.GetLastImportAsync();
                Assert.NotNull(preview);

                var result = await rollbackService.RollbackLastImportAsync();
                Assert.Equal(1, result.DeletedSubscriptionsCount);
                Assert.Equal(0, result.RestoredSubscriptionsCount);
                Assert.Equal(1, result.DeletedCategoriesCount);
            }

            await using (var finalContext = database.CreateContext())
            {
                Assert.Empty(await finalContext.Subscriptions.ToListAsync());
                Assert.DoesNotContain(await finalContext.Categories.Select(static item => item.Name).ToListAsync(), static name => name == "Work");
                Assert.Empty(await finalContext.ImportSessions.ToListAsync());
            }
        }
        finally
        {
            CleanupTempFile(csvPath);
        }
    }

    [Fact]
    public async Task RollbackLastImportAsync_RestoresUpdatedSubscriptionSnapshot()
    {
        using var database = new SqliteTestDbContextFactory();
        var csvPath = CreateTempFile(".csv");
        var categoryId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        try
        {
            await using (var seedContext = database.CreateContext())
            {
                seedContext.Categories.Add(new Category
                {
                    Id = categoryId,
                    Name = "Software",
                    ColorHex = "#94A3B8",
                    IsSystem = false
                });

                seedContext.Subscriptions.Add(new Subscription
                {
                    Id = subscriptionId,
                    Name = "Notion Plus",
                    CategoryId = categoryId,
                    Amount = 8m,
                    Currency = "USD",
                    BillingCycle = BillingCycle.Monthly,
                    FirstPaymentDate = new DateOnly(2026, 6, 1),
                    NextPaymentDate = new DateOnly(2026, 6, 1),
                    IsActive = true,
                    AutoRenewal = true,
                    ReminderDaysBefore = 2,
                    IsLowUsage = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    Payments =
                    [
                        new PaymentHistory
                        {
                            Id = Guid.NewGuid(),
                            SubscriptionId = subscriptionId,
                            Amount = 8m,
                            Currency = "USD",
                            PaymentDate = new DateOnly(2026, 6, 1),
                            Status = PaymentStatus.Planned,
                            CreatedAtUtc = DateTime.UtcNow
                        }
                    ]
                });

                await seedContext.SaveChangesAsync();
            }

            await File.WriteAllTextAsync(csvPath,
                "Name;Category;Amount;Currency;Cycle;Next payment;IsActive;IsLowUsage" + Environment.NewLine +
                "Notion Plus;Software;12;USD;Monthly;2026-07-01;false;true");

            await using (var importContext = database.CreateContext())
            {
                var importService = CreateImportService(importContext);
                await importService.ImportAsync(csvPath);
            }

            await using (var rollbackContext = database.CreateContext())
            {
                var rollbackService = new ImportRollbackService(rollbackContext);
                var result = await rollbackService.RollbackLastImportAsync();
                Assert.Equal(0, result.DeletedSubscriptionsCount);
                Assert.Equal(1, result.RestoredSubscriptionsCount);
            }

            await using (var finalContext = database.CreateContext())
            {
                var subscription = await finalContext.Subscriptions.Include(static item => item.Payments).SingleAsync();
                Assert.Equal(8m, subscription.Amount);
                Assert.True(subscription.IsActive);
                Assert.False(subscription.IsLowUsage);
                Assert.Equal(new DateOnly(2026, 6, 1), subscription.NextPaymentDate);
                Assert.Contains(subscription.Payments, static payment => payment.Status == PaymentStatus.Planned && payment.PaymentDate == new DateOnly(2026, 6, 1));
            }
        }
        finally
        {
            CleanupTempFile(csvPath);
        }
    }

    private static SubscriptionImportService CreateImportService(AppDbContext context)
    {
        var subscriptionService = new SubscriptionService(context, new TestAppSettingsService(new AppSettingsDto
        {
            BaseCurrency = "RUB"
        }));

        return new SubscriptionImportService(context, subscriptionService);
    }

    private static string CreateTempFile(string extension)
    {
        var root = Path.Combine(Path.GetTempPath(), "SubscriptionTracker.Tests", "ImportRollbackServiceTests");
        Directory.CreateDirectory(root);
        return Path.Combine(root, Guid.NewGuid().ToString("N") + extension);
    }

    private static void CleanupTempFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
