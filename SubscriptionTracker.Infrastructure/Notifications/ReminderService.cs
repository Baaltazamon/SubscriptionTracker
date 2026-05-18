using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure.Notifications;

public sealed class ReminderService(AppDbContext dbContext) : IReminderService
{
    public async Task<IReadOnlyList<ReminderNotificationDto>> GetUpcomingRemindersAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await dbContext.Subscriptions
            .AsNoTracking()
            .Where(static subscription => subscription.IsActive)
            .Where(subscription => subscription.NextPaymentDate >= today &&
                                   subscription.NextPaymentDate <= today.AddDays(subscription.ReminderDaysBefore))
            .OrderBy(static subscription => subscription.NextPaymentDate)
            .Select(static subscription => new ReminderNotificationDto
            {
                Title = subscription.Name,
                Message = LocalizationCatalog.Format(
                    "ReminderMessageFormat",
                    subscription.Amount,
                    subscription.Currency,
                    subscription.NextPaymentDate.ToDateTime(TimeOnly.MinValue)),
                PaymentDate = subscription.NextPaymentDate
            })
            .ToListAsync(cancellationToken);
    }
}
