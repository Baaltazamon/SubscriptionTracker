namespace SubscriptionTracker.Application.DTO;

public sealed class AppSettingsDto
{
    public string BaseCurrency { get; init; } = "RUB";

    public string DatabasePath { get; init; } = string.Empty;

    public bool NotificationsEnabled { get; init; } = true;

    public int ReminderCheckIntervalMinutes { get; init; } = 60;
}
