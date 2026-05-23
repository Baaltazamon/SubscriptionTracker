using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure;

public sealed class ImportSessionService(AppDbContext dbContext) : IImportSessionService
{
    public async Task<IReadOnlyList<ImportSessionListItemDto>> GetRecentAsync(int limit = 6, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 20);

        return await dbContext.ImportSessions
            .AsNoTracking()
            .OrderByDescending(static item => item.CreatedAtUtc)
            .Take(normalizedLimit)
            .Select(static item => new ImportSessionListItemDto
            {
                Id = item.Id,
                SourceFileName = item.SourceFileName,
                CreatedAtUtc = item.CreatedAtUtc,
                AppliedRowsCount = item.AppliedRowsCount,
                CreatedCount = item.CreatedCount,
                UpdatedCount = item.UpdatedCount,
                CreatedCategoryCount = item.CreatedCategoryCount
            })
            .ToListAsync(cancellationToken);
    }
}
