namespace SubscriptionTracker.Application.DTO;

public sealed class ImportSubscriptionsResultDto
{
    public int TotalRows { get; init; }

    public int CreatedCount { get; init; }

    public int UpdatedCount { get; init; }

    public int CreatedCategoryCount { get; init; }

    public int SkippedCount { get; init; }

    public int IgnoredCount { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
