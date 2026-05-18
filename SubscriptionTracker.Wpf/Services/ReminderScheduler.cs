using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.Services;

public sealed class ReminderScheduler(
    IServiceScopeFactory scopeFactory,
    IAppSettingsService settingsService,
    IToastNotificationService toastNotificationService,
    IDialogService dialogService)
{
    private readonly DispatcherTimer _timer = new();

    public async Task RunStartupCheckAsync(CancellationToken cancellationToken = default)
    {
        await CheckUpcomingPaymentsAsync(cancellationToken);
    }

    public void Start()
    {
        _timer.Interval = TimeSpan.FromMinutes(settingsService.GetSettings().ReminderCheckIntervalMinutes);
        _timer.Tick += async (_, _) => await CheckUpcomingPaymentsAsync();
        _timer.Start();
        settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, AppSettingsDto settings)
    {
        _timer.Interval = TimeSpan.FromMinutes(settings.ReminderCheckIntervalMinutes);
    }

    private async Task CheckUpcomingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        if (!settingsService.GetSettings().NotificationsEnabled)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
        var reminders = await reminderService.GetUpcomingRemindersAsync(cancellationToken);

        if (reminders.Count == 0)
        {
            return;
        }

        if (!toastNotificationService.ShowUpcomingPayments(reminders))
        {
            var message = string.Join(Environment.NewLine, reminders.Select(static item => $"{item.Title}: {item.Message}"));
            dialogService.ShowInfo(message, LocalizationCatalog.Get("ReminderNotificationTitle"));
        }
    }
}
