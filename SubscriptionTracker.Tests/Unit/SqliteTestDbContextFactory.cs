using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Tests.Unit;

internal sealed class SqliteTestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public SqliteTestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        return new AppDbContext(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
