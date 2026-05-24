using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure;

public sealed class ImportRollbackService(AppDbContext dbContext) : IImportRollbackService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ImportRollbackPreviewDto?> GetLastImportAsync(CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ImportSessions
            .AsNoTracking()
            .OrderByDescending(static item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        return new ImportRollbackPreviewDto
        {
            SessionId = session.Id,
            SourceFileName = session.SourceFileName,
            CreatedAtUtc = session.CreatedAtUtc,
            AppliedRowsCount = session.AppliedRowsCount,
            CreatedCount = session.CreatedCount,
            UpdatedCount = session.UpdatedCount,
            CreatedCategoryCount = session.CreatedCategoryCount
        };
    }

    public async Task<ImportRollbackResultDto> RollbackLastImportAsync(CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ImportSessions
            .AsNoTracking()
            .OrderByDescending(static item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(LocalizationCatalog.Get("ImportRollbackUnavailableMessage"));

        return await RollbackAsync(session.Id, cancellationToken);
    }

    public async Task<ImportRollbackResultDto> RollbackAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ImportSessions
            .Include(static item => item.Entries)
            .FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken)
            ?? throw new InvalidOperationException(LocalizationCatalog.Get("ImportRollbackUnavailableMessage"));

        var conflictingImport = await FindNewerConflictingImportAsync(session, cancellationToken);
        if (conflictingImport is not null)
        {
            throw new InvalidOperationException(LocalizationCatalog.Format(
                "ImportRollbackBlockedByNewerImport",
                conflictingImport.SourceFileName,
                conflictingImport.CreatedAtUtc.ToLocalTime()));
        }

        var createdSubscriptionEntries = session.Entries
            .Where(static entry => entry.Kind == ImportSessionEntryKind.SubscriptionCreated)
            .ToArray();

        var updatedSubscriptionEntries = session.Entries
            .Where(static entry => entry.Kind == ImportSessionEntryKind.SubscriptionUpdated)
            .ToArray();

        var createdCategoryEntries = session.Entries
            .Where(static entry => entry.Kind == ImportSessionEntryKind.CategoryCreated)
            .ToArray();
        var deletedCategoriesCount = 0;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var entry in createdSubscriptionEntries)
        {
            var subscription = await dbContext.Subscriptions
                .Include(static item => item.Payments)
                .FirstOrDefaultAsync(item => item.Id == entry.EntityId, cancellationToken);

            if (subscription is not null)
            {
                dbContext.Subscriptions.Remove(subscription);
            }
        }

        foreach (var entry in updatedSubscriptionEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.SnapshotJson))
            {
                continue;
            }

            var snapshot = JsonSerializer.Deserialize<SubscriptionImportService.SubscriptionSnapshot>(entry.SnapshotJson, SnapshotJsonOptions);
            if (snapshot is null)
            {
                continue;
            }

            var subscription = await dbContext.Subscriptions
                .FirstOrDefaultAsync(item => item.Id == snapshot.Id, cancellationToken);

            if (subscription is null)
            {
                subscription = new Subscription
                {
                    Id = snapshot.Id,
                    CreatedAtUtc = snapshot.CreatedAtUtc
                };

                await dbContext.Subscriptions.AddAsync(subscription, cancellationToken);
            }

            subscription.Name = snapshot.Name;
            subscription.Description = snapshot.Description;
            subscription.CategoryId = snapshot.CategoryId;
            subscription.Amount = snapshot.Amount;
            subscription.Currency = snapshot.Currency;
            subscription.BillingCycle = snapshot.BillingCycle;
            subscription.FirstPaymentDate = snapshot.FirstPaymentDate;
            subscription.NextPaymentDate = snapshot.NextPaymentDate;
            subscription.IsActive = snapshot.IsActive;
            subscription.AutoRenewal = snapshot.AutoRenewal;
            subscription.ReminderDaysBefore = snapshot.ReminderDaysBefore;
            subscription.IsLowUsage = snapshot.IsLowUsage;
            subscription.LastUsedDate = snapshot.LastUsedDate;
            subscription.CreatedAtUtc = snapshot.CreatedAtUtc;
            subscription.UpdatedAtUtc = snapshot.UpdatedAtUtc;

            await dbContext.PaymentHistories
                .Where(item => item.SubscriptionId == subscription.Id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var payment in snapshot.Payments)
            {
                await dbContext.PaymentHistories.AddAsync(new PaymentHistory
                {
                    Id = payment.Id,
                    SubscriptionId = subscription.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    PaymentDate = payment.PaymentDate,
                    Status = payment.Status,
                    Note = payment.Note,
                    CreatedAtUtc = payment.CreatedAtUtc
                }, cancellationToken);
            }
        }

        foreach (var entry in createdCategoryEntries)
        {
            var category = await dbContext.Categories
                .Include(static item => item.Subscriptions)
                .FirstOrDefaultAsync(item => item.Id == entry.EntityId, cancellationToken);

            if (category is not null && category.Subscriptions.Count == 0)
            {
                dbContext.Categories.Remove(category);
                deletedCategoriesCount++;
            }
        }

        dbContext.ImportSessions.Remove(session);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ImportRollbackResultDto
        {
            SourceFileName = session.SourceFileName,
            DeletedSubscriptionsCount = createdSubscriptionEntries.Length,
            RestoredSubscriptionsCount = updatedSubscriptionEntries.Length,
            DeletedCategoriesCount = deletedCategoriesCount
        };
    }

    private async Task<ImportSession?> FindNewerConflictingImportAsync(ImportSession session, CancellationToken cancellationToken)
    {
        var affectedEntityIds = session.Entries.Select(static item => item.EntityId).Distinct().ToArray();
        if (affectedEntityIds.Length == 0)
        {
            return null;
        }

        return await dbContext.ImportSessionEntries
            .AsNoTracking()
            .Where(item => affectedEntityIds.Contains(item.EntityId))
            .Where(item => item.ImportSessionId != session.Id)
            .Join(
                dbContext.ImportSessions.AsNoTracking(),
                entry => entry.ImportSessionId,
                importSession => importSession.Id,
                (entry, importSession) => importSession)
            .Where(item => item.CreatedAtUtc > session.CreatedAtUtc)
            .OrderBy(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
