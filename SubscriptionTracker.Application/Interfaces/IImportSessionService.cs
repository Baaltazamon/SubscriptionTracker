using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface IImportSessionService
{
    Task<IReadOnlyList<ImportSessionListItemDto>> GetRecentAsync(int limit = 6, CancellationToken cancellationToken = default);
}
