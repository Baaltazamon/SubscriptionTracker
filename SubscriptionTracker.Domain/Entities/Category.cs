namespace SubscriptionTracker.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ColorHex { get; set; }

    public string? Icon { get; set; }

    public bool IsSystem { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
