namespace SubscriptionTracker.Application.DTO;

public sealed class CalendarMonthDto
{
    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<UpcomingPaymentDto> Payments { get; init; } = Array.Empty<UpcomingPaymentDto>();
}
