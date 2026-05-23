namespace SubscriptionTracker.Application.DTO;

public sealed class ImportRollbackPreviewDto
{
    public Guid SessionId { get; init; }

    public string SourceFileName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public int AppliedRowsCount { get; init; }

    public int CreatedCount { get; init; }

    public int UpdatedCount { get; init; }

    public int CreatedCategoryCount { get; init; }
}
