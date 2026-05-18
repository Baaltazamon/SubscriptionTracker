namespace SubscriptionTracker.Application.DTO;

public sealed class SaveCategoryRequest
{
    public Guid? Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? ColorHex { get; init; }
}
