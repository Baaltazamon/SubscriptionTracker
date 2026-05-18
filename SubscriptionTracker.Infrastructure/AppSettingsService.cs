using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using System.Text.Json;

namespace SubscriptionTracker.Infrastructure;

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsPath;
    private AppSettingsDto _settings;

    public event EventHandler<AppSettingsDto>? SettingsChanged;

    public AppSettingsService()
    {
        var appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SubscriptionTracker");
        Directory.CreateDirectory(appDirectory);
        _settingsPath = Path.Combine(appDirectory, "settings.json");

        _settings = LoadSettings(appDirectory);
    }

    public AppSettingsDto GetSettings()
    {
        return _settings with { };
    }

    public async Task SaveAsync(AppSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _settings = Normalize(settings);

        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, _settings, cancellationToken: cancellationToken);

        SettingsChanged?.Invoke(this, GetSettings());
    }

    private AppSettingsDto LoadSettings(string appDirectory)
    {
        var defaults = new AppSettingsDto
        {
            BaseCurrency = "RUB",
            DatabasePath = Path.Combine(appDirectory, "subscription_tracker.db"),
            NotificationsEnabled = true,
            ReminderCheckIntervalMinutes = 60,
            LanguageCode = "ru-RU",
            Theme = "Dark"
        };

        if (!File.Exists(_settingsPath))
        {
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettingsDto>(json);
            return Normalize(loaded ?? defaults);
        }
        catch
        {
            return defaults;
        }
    }

    private static AppSettingsDto Normalize(AppSettingsDto settings)
    {
        var currency = string.IsNullOrWhiteSpace(settings.BaseCurrency)
            ? "RUB"
            : settings.BaseCurrency.Trim().ToUpperInvariant();

        var interval = settings.ReminderCheckIntervalMinutes <= 0
            ? 60
            : settings.ReminderCheckIntervalMinutes;

        var theme = string.Equals(settings.Theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? "Light"
            : "Dark";

        return settings with
        {
            BaseCurrency = currency,
            DatabasePath = string.IsNullOrWhiteSpace(settings.DatabasePath) ? settings.DatabasePath : settings.DatabasePath.Trim(),
            ReminderCheckIntervalMinutes = interval,
            LanguageCode = LocalizationCatalog.NormalizeLanguageCode(settings.LanguageCode),
            Theme = theme
        };
    }
}
