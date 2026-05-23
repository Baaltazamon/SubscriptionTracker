using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Wpf.Services;

public interface IImportPreviewDialogService
{
    Task<IReadOnlyList<int>?> ShowAsync(ImportSubscriptionsPreviewDto preview, CancellationToken cancellationToken = default);
}
