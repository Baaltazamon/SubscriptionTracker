using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Services;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure;

public sealed class PaymentHistoryService(AppDbContext dbContext) : IPaymentHistoryService
{
    public async Task<IReadOnlyList<PaymentHistoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PaymentHistories
            .AsNoTracking()
            .Include(static payment => payment.Subscription)
            .OrderByDescending(static payment => payment.PaymentDate)
            .Select(static payment => new PaymentHistoryDto
            {
                Id = payment.Id,
                SubscriptionId = payment.SubscriptionId,
                SubscriptionName = payment.Subscription.Name,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentDate = payment.PaymentDate,
                Status = payment.Status,
                Note = payment.Note ?? string.Empty,
                AmountLabel = $"{payment.Amount:N2} {payment.Currency}",
                PaymentDateLabel = payment.PaymentDate.ToString("dd.MM.yyyy"),
                StatusLabel = BillingCycleDisplayFormatter.ToLabel(payment.Status)
            })
            .ToListAsync(cancellationToken);
    }
}
