namespace SubscriptionTracker.Application.DTO;

public sealed class ImportSubscriptionsPreviewDto
{
    public int TotalRows { get; init; }

    public int CreatedCount { get; init; }

    public int UpdatedCount { get; init; }

    public int CreatedCategoryCount { get; init; }

    public int SkippedCount { get; init; }

    public IReadOnlyList<ImportPreviewItemDto> Items { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public bool CanImport => CreatedCount > 0 || UpdatedCount > 0;
}
