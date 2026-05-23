namespace SubscriptionTracker.Application.DTO;

public sealed class ImportRollbackResultDto
{
    public string SourceFileName { get; init; } = string.Empty;

    public int DeletedSubscriptionsCount { get; init; }

    public int RestoredSubscriptionsCount { get; init; }

    public int DeletedCategoriesCount { get; init; }
}
