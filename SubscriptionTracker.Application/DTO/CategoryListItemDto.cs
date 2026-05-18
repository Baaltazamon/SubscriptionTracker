namespace SubscriptionTracker.Application.DTO;

public sealed class CategoryListItemDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? ColorHex { get; init; }

    public bool IsSystem { get; init; }

    public int SubscriptionCount { get; init; }
}
