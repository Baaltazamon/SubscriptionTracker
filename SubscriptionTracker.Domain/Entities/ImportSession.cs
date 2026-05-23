namespace SubscriptionTracker.Domain.Entities;

public sealed class ImportSession
{
    public Guid Id { get; set; }

    public string SourceFileName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int AppliedRowsCount { get; set; }

    public int CreatedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int CreatedCategoryCount { get; set; }

    public ICollection<ImportSessionEntry> Entries { get; set; } = new List<ImportSessionEntry>();
}
