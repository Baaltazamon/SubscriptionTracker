using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Wpf.Services;

public interface IImportPreviewDialogService
{
    Task<bool> ShowAsync(ImportSubscriptionsPreviewDto preview, CancellationToken cancellationToken = default);
}
