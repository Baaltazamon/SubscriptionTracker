namespace SubscriptionTracker.Application.DTO;

public sealed class DashboardSummaryDto
{
    public int ActiveSubscriptionsCount { get; init; }

    public decimal MonthlyTotal { get; init; }

    public decimal YearlyTotal { get; init; }

    public UpcomingPaymentDto? NextPayment { get; init; }

    public string MostExpensiveSubscriptionName { get; init; } = "—";

    public decimal DailySpend { get; init; }

    public decimal PotentialSavingsMonthly { get; init; }

    public string BaseCurrency { get; init; } = "RUB";

    public IReadOnlyList<UpcomingPaymentDto> UpcomingPayments { get; init; } = Array.Empty<UpcomingPaymentDto>();

    public IReadOnlyList<CategoryExpenseDto> CategoryExpenses { get; init; } = Array.Empty<CategoryExpenseDto>();

    public IReadOnlyList<MonthlyForecastPointDto> MonthlyForecast { get; init; } = Array.Empty<MonthlyForecastPointDto>();
}
