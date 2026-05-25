using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure;

public sealed class ImportSessionService(AppDbContext dbContext) : IImportSessionService
{
    public async Task<IReadOnlyList<ImportSessionListItemDto>> GetRecentAsync(int limit = 6, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 20);

        var sessions = await dbContext.ImportSessions
            .AsNoTracking()
            .Include(static item => item.Entries)
            .OrderByDescending(static item => item.CreatedAtUtc)
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);

        var result = new List<ImportSessionListItemDto>(sessions.Count);
        foreach (var session in sessions)
        {
            var rollbackStatus = await EvaluateRollbackStatusAsync(session, cancellationToken);
            result.Add(new ImportSessionListItemDto
            {
                Id = session.Id,
                SourceFileName = session.SourceFileName,
                CreatedAtUtc = session.CreatedAtUtc,
                AppliedRowsCount = session.AppliedRowsCount,
                CreatedCount = session.CreatedCount,
                UpdatedCount = session.UpdatedCount,
                CreatedCategoryCount = session.CreatedCategoryCount,
                CanRollback = rollbackStatus.CanRollback,
                RollbackBlockedReason = rollbackStatus.BlockedReason
            });
        }

        return result;
    }

    public async Task<ImportSessionDetailsDto?> GetDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ImportSessions
            .AsNoTracking()
            .Include(static item => item.Entries)
            .FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        var rollbackStatus = await EvaluateRollbackStatusAsync(session, cancellationToken);

        return new ImportSessionDetailsDto
        {
            Id = session.Id,
            SourceFileName = session.SourceFileName,
            CreatedAtUtc = session.CreatedAtUtc,
            AppliedRowsCount = session.AppliedRowsCount,
            CreatedCount = session.CreatedCount,
            UpdatedCount = session.UpdatedCount,
            CreatedCategoryCount = session.CreatedCategoryCount,
            CanRollback = rollbackStatus.CanRollback,
            RollbackBlockedReason = rollbackStatus.BlockedReason,
            Entries = session.Entries
                .OrderBy(static item => item.Kind)
                .ThenBy(static item => item.DisplayName)
                .Select(static item => new ImportSessionEntryItemDto
                {
                    DisplayName = item.DisplayName ?? item.EntityId.ToString("N"),
                    KindLabel = item.Kind switch
                    {
                        ImportSessionEntryKind.SubscriptionCreated => LocalizationCatalog.Get("ImportEntryCreatedSubscription"),
                        ImportSessionEntryKind.SubscriptionUpdated => LocalizationCatalog.Get("ImportEntryUpdatedSubscription"),
                        ImportSessionEntryKind.CategoryCreated => LocalizationCatalog.Get("ImportEntryCreatedCategory"),
                        _ => LocalizationCatalog.Get("Unknown")
                    }
                })
                .ToArray()
        };
    }

    private async Task<(bool CanRollback, string? BlockedReason)> EvaluateRollbackStatusAsync(ImportSession session, CancellationToken cancellationToken)
    {
        var affectedEntityIds = session.Entries.Select(static item => item.EntityId).Distinct().ToArray();
        if (affectedEntityIds.Length == 0)
        {
            return (true, null);
        }

        var conflictingSession = await dbContext.ImportSessionEntries
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

        return conflictingSession is null
            ? (true, null)
            : (false, LocalizationCatalog.Format(
                "ImportRollbackBlockedByNewerImport",
                conflictingSession.SourceFileName,
                conflictingSession.CreatedAtUtc.ToLocalTime()));
    }
}
