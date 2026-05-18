using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarMonthDto>> GetCalendarAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionListItemDto> SaveAsync(SaveSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task MarkAsPaidAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    Task SkipNextPaymentAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    Task ToggleActiveAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
