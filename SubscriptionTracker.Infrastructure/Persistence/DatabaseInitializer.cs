using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Infrastructure.Persistence;

public sealed class DatabaseInitializer(AppDbContext dbContext)
{
    private static readonly Guid StreamingCategoryId = Guid.Parse("A1F536BA-AC0A-48EB-A981-467A8A5D5627");
    private static readonly Guid AiCategoryId = Guid.Parse("6543A42E-9F1F-4C9B-9F67-8332A5EA7F5E");
    private static readonly Guid HostingCategoryId = Guid.Parse("A927E9AC-6B88-483B-AEAA-4C74A97CA5B0");
    private static readonly Guid FinanceCategoryId = Guid.Parse("2A1A3720-7F11-4668-B76E-E26A2FB66112");
    private static readonly Guid SecurityCategoryId = Guid.Parse("C82FCD88-43B5-45FE-B5D4-90E0E60E710A");
    private static readonly Guid OtherCategoryId = Guid.Parse("2F35FD7B-178B-4AC2-A68C-AF87437303FE");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureSubscriptionSchemaAsync(cancellationToken);

        if (!await dbContext.Categories.AnyAsync(cancellationToken))
        {
            await dbContext.Categories.AddRangeAsync(GetDefaultCategories(), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Subscriptions.AnyAsync(cancellationToken))
        {
            await dbContext.Subscriptions.AddRangeAsync(GetSampleSubscriptions(), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureSubscriptionSchemaAsync(CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync("Subscriptions", "IsLowUsage", cancellationToken))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE Subscriptions ADD COLUMN IsLowUsage INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);
    }

    private async Task<bool> ColumnExistsAsync(string tableName, string columnName, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = false;

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            shouldClose = true;
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static IEnumerable<Category> GetDefaultCategories()
    {
        return
        [
            new Category { Id = StreamingCategoryId, Name = "Стриминг", ColorHex = "#F97316", Icon = "Tv", IsSystem = true },
            new Category { Id = AiCategoryId, Name = "AI-инструменты", ColorHex = "#0EA5E9", Icon = "Sparkles", IsSystem = true },
            new Category { Id = HostingCategoryId, Name = "Домены и хостинг", ColorHex = "#22C55E", Icon = "Server", IsSystem = true },
            new Category { Id = FinanceCategoryId, Name = "Кредиты и рассрочки", ColorHex = "#EF4444", Icon = "Wallet", IsSystem = true },
            new Category { Id = SecurityCategoryId, Name = "Безопасность", ColorHex = "#8B5CF6", Icon = "Shield", IsSystem = true },
            new Category { Id = OtherCategoryId, Name = "Прочее", ColorHex = "#94A3B8", Icon = "Grid", IsSystem = true }
        ];
    }

    private static IEnumerable<Subscription> GetSampleSubscriptions()
    {
        var createdAtUtc = DateTime.UtcNow;

        return
        [
            CreateSubscription("ChatGPT Plus", "Основная AI-подписка", AiCategoryId, 20m, "USD", BillingCycle.Monthly, new DateOnly(2026, 5, 17), new DateOnly(2026, 6, 17), 3, createdAtUtc),
            CreateSubscription("Spotify", "Семейный музыкальный тариф", StreamingCategoryId, 10.99m, "USD", BillingCycle.Monthly, new DateOnly(2026, 5, 21), new DateOnly(2026, 5, 21), 2, createdAtUtc, isLowUsage: true),
            CreateSubscription("Домен", "Продление основного домена", HostingCategoryId, 1200m, "RUB", BillingCycle.Yearly, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), 14, createdAtUtc),
            CreateSubscription("Хостинг", "VPS для pet-проектов", HostingCategoryId, 600m, "RUB", BillingCycle.Monthly, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), 5, createdAtUtc, isLowUsage: true)
        ];
    }

    private static Subscription CreateSubscription(
        string name,
        string description,
        Guid categoryId,
        decimal amount,
        string currency,
        BillingCycle cycle,
        DateOnly firstPaymentDate,
        DateOnly nextPaymentDate,
        int reminderDaysBefore,
        DateTime createdAtUtc,
        bool isLowUsage = false)
    {
        var subscriptionId = Guid.NewGuid();

        return new Subscription
        {
            Id = subscriptionId,
            Name = name,
            Description = description,
            CategoryId = categoryId,
            Amount = amount,
            Currency = currency,
            BillingCycle = cycle,
            FirstPaymentDate = firstPaymentDate,
            NextPaymentDate = nextPaymentDate,
            IsActive = true,
            AutoRenewal = true,
            ReminderDaysBefore = reminderDaysBefore,
            IsLowUsage = isLowUsage,
            CreatedAtUtc = createdAtUtc,
            Payments =
            [
                new PaymentHistory
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscriptionId,
                    Amount = amount,
                    Currency = currency,
                    PaymentDate = nextPaymentDate,
                    Status = PaymentStatus.Planned,
                    CreatedAtUtc = createdAtUtc
                }
            ]
        };
    }
}
