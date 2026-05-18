using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Domain.Services;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure;

public sealed class SubscriptionService(AppDbContext dbContext, IAppSettingsService settingsService) : ISubscriptionService
{
    public async Task<IReadOnlyList<SubscriptionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = settingsService.GetSettings();
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await dbContext.Subscriptions
            .AsNoTracking()
            .Include(static subscription => subscription.Category)
            .OrderByDescending(static subscription => subscription.IsActive)
            .ThenBy(static subscription => subscription.NextPaymentDate)
            .Select(subscription => new SubscriptionListItemDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                Description = subscription.Description ?? string.Empty,
                CategoryId = subscription.CategoryId,
                CategoryName = subscription.Category.Name,
                Amount = subscription.Amount,
                Currency = subscription.Currency,
                BillingCycle = subscription.BillingCycle,
                FirstPaymentDate = subscription.FirstPaymentDate,
                NextPaymentDate = subscription.NextPaymentDate,
                IsActive = subscription.IsActive,
                AutoRenewal = subscription.AutoRenewal,
                ReminderDaysBefore = subscription.ReminderDaysBefore,
                MonthlyCostInBaseCurrency = CurrencyConverter.Convert(
                    RecurringPaymentCalculator.GetMonthlyCost(subscription.Amount, subscription.BillingCycle),
                    subscription.Currency,
                    settings.BaseCurrency),
                BaseCurrency = settings.BaseCurrency,
                DueSoon = subscription.IsActive && subscription.NextPaymentDate <= today.AddDays(7)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarMonthDto>> GetCalendarAsync(CancellationToken cancellationToken = default)
    {
        var settings = settingsService.GetSettings();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var limit = today.AddMonths(12);
        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(static subscription => subscription.Category)
            .Where(static subscription => subscription.IsActive)
            .ToListAsync(cancellationToken);

        var items = new List<UpcomingPaymentDto>();

        foreach (var subscription in subscriptions)
        {
            var occurrence = subscription.NextPaymentDate;
            while (occurrence <= limit)
            {
                items.Add(new UpcomingPaymentDto
                {
                    SubscriptionId = subscription.Id,
                    SubscriptionName = subscription.Name,
                    CategoryName = subscription.Category.Name,
                    Amount = subscription.Amount,
                    Currency = subscription.Currency,
                    PaymentDate = occurrence,
                    Status = PaymentStatus.Planned,
                    AmountInBaseCurrency = CurrencyConverter.Convert(subscription.Amount, subscription.Currency, settings.BaseCurrency),
                    BaseCurrency = settings.BaseCurrency
                });

                occurrence = RecurringPaymentCalculator.GetNextDate(occurrence, subscription.BillingCycle);
            }
        }

        return items
            .OrderBy(static item => item.PaymentDate)
            .GroupBy(item => new DateOnly(item.PaymentDate.Year, item.PaymentDate.Month, 1))
            .Select(group => new CalendarMonthDto
            {
                Title = group.Key.ToString("MMMM yyyy"),
                Payments = group.ToArray()
            })
            .ToArray();
    }

    public async Task<SubscriptionListItemDto> SaveAsync(SaveSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("SubscriptionNameRequired"));
        }

        if (request.Amount <= 0m)
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("AmountMustBePositive"));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var normalizedNextDate = RecurringPaymentCalculator.NormalizeFutureDate(
            request.FirstPaymentDate,
            request.NextPaymentDate,
            request.BillingCycle,
            today);

        Subscription entity;
        if (request.Id is { } id)
        {
            entity = await dbContext.Subscriptions
                .Include(static subscription => subscription.Payments)
                .FirstAsync(subscription => subscription.Id == id, cancellationToken);

            entity.Name = request.Name.Trim();
            entity.Description = request.Description?.Trim();
            entity.CategoryId = request.CategoryId;
            entity.Amount = request.Amount;
            entity.Currency = request.Currency.Trim().ToUpperInvariant();
            entity.BillingCycle = request.BillingCycle;
            entity.FirstPaymentDate = request.FirstPaymentDate;
            entity.NextPaymentDate = normalizedNextDate;
            entity.IsActive = request.IsActive;
            entity.AutoRenewal = request.AutoRenewal;
            entity.ReminderDaysBefore = request.ReminderDaysBefore;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            var futurePlannedPayments = entity.Payments
                .Where(payment => payment.Status == PaymentStatus.Planned && payment.PaymentDate >= today)
                .ToArray();

            dbContext.PaymentHistories.RemoveRange(futurePlannedPayments);
        }
        else
        {
            entity = new Subscription
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CategoryId = request.CategoryId,
                Amount = request.Amount,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                BillingCycle = request.BillingCycle,
                FirstPaymentDate = request.FirstPaymentDate,
                NextPaymentDate = normalizedNextDate,
                IsActive = request.IsActive,
                AutoRenewal = request.AutoRenewal,
                ReminderDaysBefore = request.ReminderDaysBefore,
                CreatedAtUtc = DateTime.UtcNow
            };

            await dbContext.Subscriptions.AddAsync(entity, cancellationToken);
        }

        if (entity.IsActive)
        {
            await dbContext.PaymentHistories.AddAsync(new PaymentHistory
            {
                Id = Guid.NewGuid(),
                SubscriptionId = entity.Id,
                Amount = entity.Amount,
                Currency = entity.Currency,
                PaymentDate = entity.NextPaymentDate,
                Status = PaymentStatus.Planned,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetAllAsync(cancellationToken)).First(item => item.Id == entity.Id);
    }

    public async Task MarkAsPaidAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.Subscriptions
            .Include(static item => item.Payments)
            .FirstAsync(item => item.Id == subscriptionId, cancellationToken);

        if (!subscription.IsActive)
        {
            return;
        }

        var plannedPayment = subscription.Payments
            .Where(payment => payment.Status == PaymentStatus.Planned)
            .OrderBy(payment => payment.PaymentDate)
            .FirstOrDefault();

        if (plannedPayment is null)
        {
            return;
        }

        plannedPayment.Status = PaymentStatus.Paid;
        plannedPayment.Note = LocalizationCatalog.Get("MarkedPaidNote");
        subscription.LastUsedDate = DateOnly.FromDateTime(DateTime.Today);

        await ScheduleNextPaymentAsync(subscription, plannedPayment.PaymentDate, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SkipNextPaymentAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.Subscriptions
            .Include(static item => item.Payments)
            .FirstAsync(item => item.Id == subscriptionId, cancellationToken);

        var plannedPayment = subscription.Payments
            .Where(payment => payment.Status == PaymentStatus.Planned)
            .OrderBy(payment => payment.PaymentDate)
            .FirstOrDefault();

        if (plannedPayment is null)
        {
            return;
        }

        plannedPayment.Status = PaymentStatus.Skipped;
        plannedPayment.Note = LocalizationCatalog.Get("PaymentSkippedNote");

        await ScheduleNextPaymentAsync(subscription, plannedPayment.PaymentDate, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleActiveAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.Subscriptions
            .Include(static item => item.Payments)
            .FirstAsync(item => item.Id == subscriptionId, cancellationToken);

        subscription.IsActive = !subscription.IsActive;
        subscription.UpdatedAtUtc = DateTime.UtcNow;

        if (subscription.IsActive)
        {
            subscription.NextPaymentDate = RecurringPaymentCalculator.NormalizeFutureDate(
                subscription.FirstPaymentDate,
                subscription.NextPaymentDate,
                subscription.BillingCycle,
                DateOnly.FromDateTime(DateTime.Today));

            await dbContext.PaymentHistories.AddAsync(new PaymentHistory
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Amount = subscription.Amount,
                Currency = subscription.Currency,
                PaymentDate = subscription.NextPaymentDate,
                Status = PaymentStatus.Planned,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            foreach (var payment in subscription.Payments.Where(payment => payment.Status == PaymentStatus.Planned))
            {
                payment.Status = PaymentStatus.Cancelled;
                payment.Note = LocalizationCatalog.Get("SubscriptionDisabledNote");
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.Subscriptions
            .Include(static item => item.Payments)
            .FirstAsync(item => item.Id == subscriptionId, cancellationToken);

        dbContext.Subscriptions.Remove(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ScheduleNextPaymentAsync(Subscription subscription, DateOnly currentPaymentDate, CancellationToken cancellationToken)
    {
        var nextPaymentDate = RecurringPaymentCalculator.GetNextDate(currentPaymentDate, subscription.BillingCycle);
        subscription.NextPaymentDate = nextPaymentDate;
        subscription.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.PaymentHistories.AddAsync(new PaymentHistory
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            PaymentDate = nextPaymentDate,
            Status = PaymentStatus.Planned,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
