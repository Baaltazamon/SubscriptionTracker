using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Services;
using System.Text.Json;

namespace SubscriptionTracker.Infrastructure;

public sealed class AppSettingsService : IAppSettingsService
{
    public const string AppDataDirectoryOverrideEnvironmentVariable = "SUBSCRIPTION_TRACKER_APPDATA_DIR";

    private readonly string _settingsPath;
    private AppSettingsDto _settings;

    public event EventHandler<AppSettingsDto>? SettingsChanged;

    public AppSettingsService()
    {
        var appDirectory = ResolveAppDirectory();
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

    private static string ResolveAppDirectory()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable(AppDataDirectoryOverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return overrideDirectory.Trim();
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SubscriptionTracker");
    }

    private AppSettingsDto LoadSettings(string appDirectory)
    {
        var defaults = new AppSettingsDto
        {
            BaseCurrency = "RUB",
            ExchangeRatesToRub = CurrencyConverter.GetDefaultRatesToRub(),
            ExchangeRatesUpdatedAtUtc = null,
            DatabasePath = Path.Combine(appDirectory, "subscription_tracker.db"),
            NotificationsEnabled = true,
            ReminderCheckIntervalMinutes = 60,
            LanguageCode = "ru-RU",
            Theme = "Dark",
            LaunchOnStartup = false
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
            ExchangeRatesToRub = CurrencyConverter.NormalizeRatesToRub(settings.ExchangeRatesToRub),
            ExchangeRatesUpdatedAtUtc = settings.ExchangeRatesUpdatedAtUtc,
            DatabasePath = string.IsNullOrWhiteSpace(settings.DatabasePath) ? settings.DatabasePath : settings.DatabasePath.Trim(),
            ReminderCheckIntervalMinutes = interval,
            LanguageCode = LocalizationCatalog.NormalizeLanguageCode(settings.LanguageCode),
            Theme = theme,
            LaunchOnStartup = settings.LaunchOnStartup
        };
    }
}
