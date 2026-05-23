using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Infrastructure;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Tests.Unit;

public sealed class SubscriptionImportServiceTests
{
    [Fact]
    public async Task ImportAsync_Csv_CreatesSubscriptionsAndCategory()
    {
        using var database = new SqliteTestDbContextFactory();
        var csvPath = CreateTempFile(".csv");

        try
        {
            await File.WriteAllTextAsync(csvPath,
                "Name;Category;Amount;Currency;Cycle;Next payment;Description;IsLowUsage" + Environment.NewLine +
                "YouTube Premium;Streaming Plus;599;RUB;Monthly;2026-06-21;Family plan;true");

            await using var context = database.CreateContext();
            var service = CreateService(context);

            var result = await service.ImportAsync(csvPath);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(0, result.UpdatedCount);
            Assert.Equal(1, result.CreatedCategoryCount);
            Assert.Equal(0, result.SkippedCount);

            await using var verification = database.CreateContext();
            var subscription = await verification.Subscriptions.Include(static item => item.Category).SingleAsync();
            Assert.Equal("YouTube Premium", subscription.Name);
            Assert.Equal("Streaming Plus", subscription.Category.Name);
            Assert.True(subscription.IsLowUsage);
            Assert.Equal(BillingCycle.Monthly, subscription.BillingCycle);
        }
        finally
        {
            CleanupTempFile(csvPath);
        }
    }

    [Fact]
    public async Task ImportAsync_Excel_UpdatesExistingSubscription()
    {
        using var database = new SqliteTestDbContextFactory();
        var excelPath = CreateTempFile(".xlsx");
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

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Subscriptions");
                sheet.Cells[1, 1].Value = "Name";
                sheet.Cells[1, 2].Value = "Category";
                sheet.Cells[1, 3].Value = "Amount";
                sheet.Cells[1, 4].Value = "Currency";
                sheet.Cells[1, 5].Value = "Cycle";
                sheet.Cells[1, 6].Value = "Next payment";
                sheet.Cells[1, 7].Value = "Status";
                sheet.Cells[2, 1].Value = "Notion Plus";
                sheet.Cells[2, 2].Value = "Software";
                sheet.Cells[2, 3].Value = 12m;
                sheet.Cells[2, 4].Value = "USD";
                sheet.Cells[2, 5].Value = "Monthly";
                sheet.Cells[2, 6].Value = "2026-07-01";
                sheet.Cells[2, 7].Value = "Disabled";
                await package.SaveAsAsync(new FileInfo(excelPath));
            }

            await using var context = database.CreateContext();
            var service = CreateService(context);

            var result = await service.ImportAsync(excelPath);

            Assert.Equal(1, result.UpdatedCount);
            Assert.Equal(0, result.CreatedCount);
            Assert.Equal(0, result.SkippedCount);

            await using var verification = database.CreateContext();
            var subscription = await verification.Subscriptions.Include(static item => item.Payments).SingleAsync();
            Assert.Equal(12m, subscription.Amount);
            Assert.False(subscription.IsActive);
            Assert.Equal(new DateOnly(2026, 7, 1), subscription.NextPaymentDate);
            Assert.DoesNotContain(subscription.Payments, static payment => payment.Status == PaymentStatus.Planned);
        }
        finally
        {
            CleanupTempFile(excelPath);
        }
    }

    [Fact]
    public async Task PreviewAsync_Csv_ReturnsCreateAndSkipRows_BeforeImport()
    {
        using var database = new SqliteTestDbContextFactory();
        var csvPath = CreateTempFile(".csv");

        try
        {
            await File.WriteAllTextAsync(csvPath,
                "Name;Category;Amount;Currency;Cycle;Next payment" + Environment.NewLine +
                "Figma Pro;Design;15;USD;Monthly;2026-07-10" + Environment.NewLine +
                "Broken Row;Design;abc;USD;Monthly;2026-07-10");

            await using var context = database.CreateContext();
            var service = CreateService(context);

            var preview = await service.PreviewAsync(csvPath);

            Assert.Equal(2, preview.TotalRows);
            Assert.Equal(1, preview.CreatedCount);
            Assert.Equal(0, preview.UpdatedCount);
            Assert.Equal(1, preview.CreatedCategoryCount);
            Assert.Equal(1, preview.SkippedCount);
            Assert.True(preview.CanImport);
            Assert.Equal(2, preview.Items.Count);
            Assert.Contains(preview.Items, static item => item.Action == ImportPreviewAction.Create && item.Name == "Figma Pro");
            Assert.Contains(preview.Items, static item => item.Action == ImportPreviewAction.Skip);
            Assert.NotEmpty(preview.Warnings);
        }
        finally
        {
            CleanupTempFile(csvPath);
        }
    }

    [Fact]
    public async Task ImportAsync_Csv_ImportsOnlySelectedRows()
    {
        using var database = new SqliteTestDbContextFactory();
        var csvPath = CreateTempFile(".csv");

        try
        {
            await File.WriteAllTextAsync(csvPath,
                "Name;Category;Amount;Currency;Cycle;Next payment" + Environment.NewLine +
                "Figma Pro;Design;15;USD;Monthly;2026-07-10" + Environment.NewLine +
                "Miro;Collaboration;12;USD;Monthly;2026-07-12");

            await using var context = database.CreateContext();
            var service = CreateService(context);

            var preview = await service.PreviewAsync(csvPath);
            var selectedRows = preview.Items
                .Where(static item => item.Name == "Miro")
                .Select(static item => item.RowNumber)
                .ToArray();

            var result = await service.ImportAsync(csvPath, selectedRows);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(0, result.UpdatedCount);
            Assert.Equal(1, result.IgnoredCount);
            Assert.Equal(0, result.SkippedCount);

            await using var verification = database.CreateContext();
            var subscriptions = await verification.Subscriptions.OrderBy(static item => item.Name).ToListAsync();
            Assert.Single(subscriptions);
            Assert.Equal("Miro", subscriptions[0].Name);
        }
        finally
        {
            CleanupTempFile(csvPath);
        }
    }

    private static SubscriptionImportService CreateService(AppDbContext context)
    {
        var subscriptionService = new SubscriptionService(context, new TestAppSettingsService(new AppSettingsDto
        {
            BaseCurrency = "RUB"
        }));

        return new SubscriptionImportService(context, subscriptionService);
    }

    private static string CreateTempFile(string extension)
    {
        var root = Path.Combine(Path.GetTempPath(), "SubscriptionTracker.Tests", "SubscriptionImportServiceTests");
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
