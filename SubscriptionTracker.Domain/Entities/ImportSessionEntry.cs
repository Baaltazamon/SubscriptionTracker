using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Domain.Entities;

public sealed class ImportSessionEntry
{
    public Guid Id { get; set; }

    public Guid ImportSessionId { get; set; }

    public ImportSession ImportSession { get; set; } = null!;

    public ImportSessionEntryKind Kind { get; set; }

    public Guid EntityId { get; set; }

    public string? SnapshotJson { get; set; }

    public string? DisplayName { get; set; }
}
