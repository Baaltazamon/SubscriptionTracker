using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetAsync(CancellationToken cancellationToken = default);
}
