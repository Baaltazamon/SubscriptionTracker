namespace SubscriptionTracker.Application.DTO;

public sealed class DeleteCategoryRequest
{
    public Guid CategoryId { get; init; }

    public Guid? TransferCategoryId { get; init; }
}
