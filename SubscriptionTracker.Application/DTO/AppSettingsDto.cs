namespace SubscriptionTracker.Application.DTO;

public sealed record AppSettingsDto
{
    public string BaseCurrency { get; init; } = "RUB";

    public string DatabasePath { get; init; } = string.Empty;

    public bool NotificationsEnabled { get; init; } = true;

    public int ReminderCheckIntervalMinutes { get; init; } = 60;

    public string LanguageCode { get; init; } = "ru-RU";

    public string Theme { get; init; } = "Dark";

    public bool LaunchOnStartup { get; init; }
}
