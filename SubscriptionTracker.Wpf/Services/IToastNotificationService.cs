using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Wpf.Services;

public interface IToastNotificationService
{
    bool ShowUpcomingPayments(IReadOnlyList<ReminderNotificationDto> reminders);
}
