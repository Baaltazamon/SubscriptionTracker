using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Infrastructure;
using SubscriptionTracker.Infrastructure.Notifications;
using SubscriptionTracker.Infrastructure.Persistence;
using SubscriptionTracker.Infrastructure.Reports;

namespace SubscriptionTracker.Tests.Unit;

public sealed class ReleaseSmokeScenarioTests
{
    [Fact]
    public async Task DatabaseInitializer_SeedsDefaultData_OnFirstRun()
    {
        var databasePath = CreateFilePath(".db");

        try
        {
            await using (var context = CreateSqliteContext(databasePath))
            {
                var initializer = new DatabaseInitializer(context);

                await initializer.InitializeAsync();

                Assert.True(File.Exists(databasePath));
                Assert.Equal(6, await context.Categories.CountAsync());
                Assert.Equal(4, await context.Subscriptions.CountAsync());
                Assert.True(await context.PaymentHistories.AnyAsync());
            }
        }
        finally
        {
            CleanupFileSet(databasePath);
        }
    }

    [Fact]
    public async Task ReminderService_ReturnsUpcomingSeededPayments()
    {
        var databasePath = CreateFilePath(".db");

        try
        {
            await using (var context = CreateSqliteContext(databasePath))
            {
                var initializer = new DatabaseInitializer(context);
                await initializer.InitializeAsync();

                var service = new ReminderService(context);
                var reminders = await service.GetUpcomingRemindersAsync();

                Assert.NotEmpty(reminders);
                Assert.All(reminders, static item =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(item.Title));
                    Assert.False(string.IsNullOrWhiteSpace(item.Message));
                });
            }
        }
        finally
        {
            CleanupFileSet(databasePath);
        }
    }

    [Fact]
    public async Task ExcelExportService_CreatesWorkbook_WithExpectedSheets()
    {
        var databasePath = CreateFilePath(".db");
        var exportPath = CreateFilePath(".xlsx");

        try
        {
            await using (var context = CreateSqliteContext(databasePath))
            {
                var initializer = new DatabaseInitializer(context);
                await initializer.InitializeAsync();

                var service = new ExcelExportService(context);
                await service.ExportAsync(exportPath);
            }

            Assert.True(File.Exists(exportPath));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(exportPath)))
            {
                Assert.Equal(2, package.Workbook.Worksheets.Count);
                Assert.False(string.IsNullOrWhiteSpace(package.Workbook.Worksheets[0].Name));
                Assert.False(string.IsNullOrWhiteSpace(package.Workbook.Worksheets[1].Name));
                Assert.NotNull(package.Workbook.Worksheets[0].Cells[1, 1].Value);
                Assert.NotNull(package.Workbook.Worksheets[1].Cells[1, 1].Value);
            }
        }
        finally
        {
            CleanupFileSet(databasePath);
            CleanupFileSet(exportPath);
        }
    }

    [Fact]
    public async Task DatabaseBackupService_CreatesAndRestoresBackup()
    {
        var databasePath = CreateFilePath(".db");
        var backupPath = CreateFilePath(".sqlite");

        try
        {
            await using (var context = CreateSqliteContext(databasePath))
            {
                var initializer = new DatabaseInitializer(context);
                await initializer.InitializeAsync();
            }

            var settingsService = new TestAppSettingsService(new AppSettingsDto
            {
                BaseCurrency = "RUB",
                DatabasePath = databasePath,
                NotificationsEnabled = true,
                ReminderCheckIntervalMinutes = 60,
                LanguageCode = "ru-RU",
                Theme = "Dark",
                LaunchOnStartup = false
            });

            var service = new DatabaseBackupService(settingsService);
            await service.CreateBackupAsync(backupPath);

            Assert.True(File.Exists(backupPath));

            await using (var mutateContext = CreateSqliteContext(databasePath))
            {
                mutateContext.Subscriptions.RemoveRange(mutateContext.Subscriptions);
                await mutateContext.SaveChangesAsync();
                Assert.Equal(0, await mutateContext.Subscriptions.CountAsync());
            }

            await service.RestoreBackupAsync(backupPath);

            await using (var restoredContext = CreateSqliteContext(databasePath))
            {
                Assert.Equal(4, await restoredContext.Subscriptions.CountAsync());
            }
        }
        finally
        {
            CleanupFileSet(databasePath);
            CleanupFileSet(backupPath);
        }
    }

    private static AppDbContext CreateSqliteContext(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default
        }.ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string CreateFilePath(string extension)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ReleaseSmokeScenarioTests");
        Directory.CreateDirectory(root);
        return Path.Combine(root, $"{Guid.NewGuid():N}{extension}");
    }

    private static void CleanupFileSet(string path)
    {
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            var candidate = path + suffix;
            if (File.Exists(candidate))
            {
                TryDelete(candidate);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
