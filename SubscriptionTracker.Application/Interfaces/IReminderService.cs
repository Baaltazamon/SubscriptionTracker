using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface IReminderService
{
    Task<IReadOnlyList<ReminderNotificationDto>> GetUpcomingRemindersAsync(CancellationToken cancellationToken = default);
}
