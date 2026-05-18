namespace SubscriptionTracker.Application.DTO;

public sealed class ReminderNotificationDto
{
    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateOnly PaymentDate { get; init; }
}
