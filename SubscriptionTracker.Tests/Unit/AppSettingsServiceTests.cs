using SubscriptionTracker.Infrastructure;

namespace SubscriptionTracker.Tests.Unit;

public sealed class AppSettingsServiceTests
{
    [Fact]
    public void GetSettings_UsesOverrideDirectory_WhenEnvironmentVariableIsSet()
    {
        var testDirectory = CreateTestDirectory();
        var previousValue = Environment.GetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable, testDirectory);

            var service = new AppSettingsService();
            var settings = service.GetSettings();

            Assert.Equal(Path.Combine(testDirectory, "subscription_tracker.db"), settings.DatabasePath);
            Assert.Equal(1m, settings.ExchangeRatesToRub["RUB"]);
            Assert.Equal(92m, settings.ExchangeRatesToRub["USD"]);
            Assert.False(File.Exists(Path.Combine(testDirectory, "settings.json")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable, previousValue);
            CleanupTestDirectory(testDirectory);
        }
    }

    [Fact]
    public async Task SaveAsync_PersistsSettingsInsideOverrideDirectory()
    {
        var testDirectory = CreateTestDirectory();
        var previousValue = Environment.GetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable, testDirectory);

            var service = new AppSettingsService();
            var settings = service.GetSettings() with
            {
                BaseCurrency = "usd",
                ExchangeRatesToRub = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RUB"] = 5m,
                    ["USD"] = 95.5m,
                    ["EUR"] = 103.2m,
                    ["GBP"] = 121m
                },
                ExchangeRatesUpdatedAtUtc = new DateTime(2026, 5, 20, 12, 30, 0, DateTimeKind.Utc),
                LanguageCode = "en",
                Theme = "Light",
                ReminderCheckIntervalMinutes = 15,
                NotificationsEnabled = false,
                LaunchOnStartup = true
            };

            await service.SaveAsync(settings);

            var reloaded = new AppSettingsService().GetSettings();

            Assert.Equal("USD", reloaded.BaseCurrency);
            Assert.Equal(1m, reloaded.ExchangeRatesToRub["RUB"]);
            Assert.Equal(95.5m, reloaded.ExchangeRatesToRub["USD"]);
            Assert.Equal(103.2m, reloaded.ExchangeRatesToRub["EUR"]);
            Assert.Equal(new DateTime(2026, 5, 20, 12, 30, 0, DateTimeKind.Utc), reloaded.ExchangeRatesUpdatedAtUtc);
            Assert.Equal("en-US", reloaded.LanguageCode);
            Assert.Equal("Light", reloaded.Theme);
            Assert.Equal(15, reloaded.ReminderCheckIntervalMinutes);
            Assert.False(reloaded.NotificationsEnabled);
            Assert.True(reloaded.LaunchOnStartup);
            Assert.True(File.Exists(Path.Combine(testDirectory, "settings.json")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable, previousValue);
            CleanupTestDirectory(testDirectory);
        }
    }

    [Fact]
    public async Task SaveAsync_NormalizesInvalidExchangeRates()
    {
        var testDirectory = CreateTestDirectory();
        var previousValue = Environment.GetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable, testDirectory);

            var service = new AppSettingsService();
            var settings = service.GetSettings() with
            {
                ExchangeRatesToRub = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RUB"] = 4m,
                    ["USD"] = -7m,
                    ["EUR"] = 110m,
                    ["CAD"] = 70m
                }
            };

            await service.SaveAsync(settings);

            var reloaded = new AppSettingsService().GetSettings();

            Assert.Equal(1m, reloaded.ExchangeRatesToRub["RUB"]);
            Assert.Equal(92m, reloaded.ExchangeRatesToRub["USD"]);
            Assert.Equal(110m, reloaded.ExchangeRatesToRub["EUR"]);
            Assert.Equal(70m, reloaded.ExchangeRatesToRub["CAD"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppSettingsService.AppDataDirectoryOverrideEnvironmentVariable, previousValue);
            CleanupTestDirectory(testDirectory);
        }
    }

    private static string CreateTestDirectory()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "AppSettingsServiceTests");
        var path = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CleanupTestDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
