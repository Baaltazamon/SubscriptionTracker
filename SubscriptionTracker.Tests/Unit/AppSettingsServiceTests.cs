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
                LanguageCode = "en",
                Theme = "Light",
                ReminderCheckIntervalMinutes = 15,
                NotificationsEnabled = false,
                LaunchOnStartup = true
            };

            await service.SaveAsync(settings);

            var reloaded = new AppSettingsService().GetSettings();

            Assert.Equal("USD", reloaded.BaseCurrency);
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
