using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Wpf.Services;

public interface ISubscriptionEditorService
{
    Task<bool> ShowAsync(SubscriptionListItemDto? currentItem, CancellationToken cancellationToken = default);
}
