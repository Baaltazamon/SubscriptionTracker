using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface IPaymentHistoryService
{
    Task<IReadOnlyList<PaymentHistoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
