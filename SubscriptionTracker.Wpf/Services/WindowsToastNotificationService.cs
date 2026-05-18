using Microsoft.Toolkit.Uwp.Notifications;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.Services;

public sealed class WindowsToastNotificationService : IToastNotificationService
{
    public bool ShowUpcomingPayments(IReadOnlyList<ReminderNotificationDto> reminders)
    {
        if (reminders.Count == 0)
        {
            return true;
        }

        try
        {
            var builder = new ToastContentBuilder()
                .AddText(LocalizationCatalog.Get("ReminderNotificationTitle"));

            foreach (var reminder in reminders.Take(3))
            {
                builder.AddText(reminder.Title);
                builder.AddText(reminder.Message);
            }

            if (reminders.Count > 3)
            {
                builder.AddText(LocalizationCatalog.Format("ReminderToastMoreFormat", reminders.Count - 3));
            }

            builder.Show(static _ => { });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
