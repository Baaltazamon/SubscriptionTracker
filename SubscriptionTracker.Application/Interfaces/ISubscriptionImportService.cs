using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface ISubscriptionImportService
{
    Task<ImportSubscriptionsPreviewDto> PreviewAsync(string filePath, CancellationToken cancellationToken = default);

    Task<ImportSubscriptionsResultDto> ImportAsync(
        string filePath,
        IReadOnlyCollection<int>? selectedRowNumbers = null,
        CancellationToken cancellationToken = default);
}
