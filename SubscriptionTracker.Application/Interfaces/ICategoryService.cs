using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryOptionDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
