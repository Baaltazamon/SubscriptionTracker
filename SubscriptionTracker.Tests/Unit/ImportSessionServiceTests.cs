using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Infrastructure;

namespace SubscriptionTracker.Tests.Unit;

public sealed class ImportSessionServiceTests
{
    [Fact]
    public async Task GetRecentAsync_ReturnsLatestSessionsInDescendingOrder()
    {
        using var database = new SqliteTestDbContextFactory();

        await using (var seedContext = database.CreateContext())
        {
            seedContext.ImportSessions.AddRange(
                new ImportSession
                {
                    Id = Guid.NewGuid(),
                    SourceFileName = "old.csv",
                    CreatedAtUtc = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc),
                    AppliedRowsCount = 2,
                    CreatedCount = 1,
                    UpdatedCount = 1,
                    CreatedCategoryCount = 0
                },
                new ImportSession
                {
                    Id = Guid.NewGuid(),
                    SourceFileName = "latest.xlsx",
                    CreatedAtUtc = new DateTime(2026, 5, 22, 15, 30, 0, DateTimeKind.Utc),
                    AppliedRowsCount = 4,
                    CreatedCount = 3,
                    UpdatedCount = 1,
                    CreatedCategoryCount = 2
                },
                new ImportSession
                {
                    Id = Guid.NewGuid(),
                    SourceFileName = "middle.csv",
                    CreatedAtUtc = new DateTime(2026, 5, 21, 11, 15, 0, DateTimeKind.Utc),
                    AppliedRowsCount = 1,
                    CreatedCount = 0,
                    UpdatedCount = 1,
                    CreatedCategoryCount = 0
                });

            await seedContext.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var service = new ImportSessionService(context);

        var sessions = await service.GetRecentAsync(2);

        Assert.Equal(2, sessions.Count);
        Assert.Equal("latest.xlsx", sessions[0].SourceFileName);
        Assert.Equal("middle.csv", sessions[1].SourceFileName);
        Assert.Equal(4, sessions[0].AppliedRowsCount);
        Assert.Equal(2, sessions[0].CreatedCategoryCount);
    }

    [Fact]
    public async Task GetDetailsAsync_MarksRollbackAsBlocked_WhenNewerImportTouchesSameEntity()
    {
        using var database = new SqliteTestDbContextFactory();
        var subscriptionId = Guid.NewGuid();
        var olderSessionId = Guid.NewGuid();
        var newerSessionId = Guid.NewGuid();

        await using (var seedContext = database.CreateContext())
        {
            seedContext.ImportSessions.AddRange(
                new ImportSession
                {
                    Id = olderSessionId,
                    SourceFileName = "older.csv",
                    CreatedAtUtc = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc),
                    AppliedRowsCount = 1,
                    CreatedCount = 0,
                    UpdatedCount = 1,
                    CreatedCategoryCount = 0,
                    Entries =
                    [
                        new ImportSessionEntry
                        {
                            Id = Guid.NewGuid(),
                            Kind = Domain.Enums.ImportSessionEntryKind.SubscriptionUpdated,
                            EntityId = subscriptionId,
                            DisplayName = "Notion Plus"
                        }
                    ]
                },
                new ImportSession
                {
                    Id = newerSessionId,
                    SourceFileName = "newer.csv",
                    CreatedAtUtc = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc),
                    AppliedRowsCount = 1,
                    CreatedCount = 0,
                    UpdatedCount = 1,
                    CreatedCategoryCount = 0,
                    Entries =
                    [
                        new ImportSessionEntry
                        {
                            Id = Guid.NewGuid(),
                            Kind = Domain.Enums.ImportSessionEntryKind.SubscriptionUpdated,
                            EntityId = subscriptionId,
                            DisplayName = "Notion Plus"
                        }
                    ]
                });

            await seedContext.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var service = new ImportSessionService(context);

        var details = await service.GetDetailsAsync(olderSessionId);

        Assert.NotNull(details);
        Assert.False(details.CanRollback);
        Assert.Contains("newer.csv", details.RollbackBlockedReason);
    }
}
