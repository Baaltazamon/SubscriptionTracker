using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface IImportRollbackService
{
    Task<ImportRollbackPreviewDto?> GetLastImportAsync(CancellationToken cancellationToken = default);

    Task<ImportRollbackResultDto> RollbackLastImportAsync(CancellationToken cancellationToken = default);
}
