using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface ISubscriptionImportService
{
    Task<ImportSubscriptionsResultDto> ImportAsync(string filePath, CancellationToken cancellationToken = default);
}
