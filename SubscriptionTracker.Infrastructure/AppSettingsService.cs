using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;

namespace SubscriptionTracker.Infrastructure;

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly AppSettingsDto _settings;

    public AppSettingsService()
    {
        var appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SubscriptionTracker");
        Directory.CreateDirectory(appDirectory);

        _settings = new AppSettingsDto
        {
            BaseCurrency = "RUB",
            DatabasePath = Path.Combine(appDirectory, "subscription_tracker.db"),
            NotificationsEnabled = true,
            ReminderCheckIntervalMinutes = 60
        };
    }

    public AppSettingsDto GetSettings()
    {
        return _settings;
    }
}
