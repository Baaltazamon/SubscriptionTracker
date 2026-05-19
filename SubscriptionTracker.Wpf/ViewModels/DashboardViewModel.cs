using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class DashboardViewModel(IServiceScopeFactory scopeFactory) : ViewModelBase
{
    private DashboardSummaryDto _summary = new();

    public DashboardSummaryDto Summary
    {
        get => _summary;
        private set
        {
            if (SetProperty(ref _summary, value))
            {
                RaisePropertyChanged(nameof(MonthlyTotalLabel));
                RaisePropertyChanged(nameof(YearlyTotalLabel));
                RaisePropertyChanged(nameof(DailySpendLabel));
                RaisePropertyChanged(nameof(PotentialSavingsLabel));
                RaisePropertyChanged(nameof(NextPaymentName));
                RaisePropertyChanged(nameof(NextPaymentDateLabel));
                RaisePropertyChanged(nameof(NextPaymentAmountLabel));
                RaisePropertyChanged(nameof(NextPaymentCountdownLabel));
                RaisePropertyChanged(nameof(NextPaymentUrgencyLabel));
                RaisePropertyChanged(nameof(NextPaymentUrgencyColorHex));
                RaisePropertyChanged(nameof(ForecastPreview));
                RaisePropertyChanged(nameof(CancellationRecommendationsEmptyText));
            }
        }
    }

    public string MonthlyTotalLabel => $"{Summary.MonthlyTotal:N2} {Summary.BaseCurrency}";

    public string YearlyTotalLabel => $"{Summary.YearlyTotal:N2} {Summary.BaseCurrency}";

    public string DailySpendLabel => $"{Summary.DailySpend:N2} {Summary.BaseCurrency}";

    public string PotentialSavingsLabel => LocalizationCatalog.Format(
        "PerMonthSuffix",
        $"{Summary.PotentialSavingsMonthly:N2} {Summary.BaseCurrency}");

    public string NextPaymentName => Summary.NextPayment?.SubscriptionName ?? LocalizationCatalog.Get("NoUpcomingPayments");

    public string NextPaymentDateLabel => Summary.NextPayment?.PaymentDateLabel ?? "—";

    public string NextPaymentAmountLabel => Summary.NextPayment?.AmountLabel ?? "—";

    public string NextPaymentCountdownLabel => Summary.NextPayment?.CountdownLabel ?? LocalizationCatalog.Get("AddFirstSubscription");

    public string NextPaymentUrgencyLabel => Summary.NextPayment?.UrgencyLabel ?? LocalizationCatalog.Get("EmptyState");

    public string NextPaymentUrgencyColorHex => Summary.NextPayment?.UrgencyColorHex ?? "#475569";

    public IReadOnlyList<MonthlyForecastPointDto> ForecastPreview => Summary.MonthlyForecast.Take(6).ToArray();

    public string CancellationRecommendationsEmptyText => Summary.CancellationRecommendations.Count == 0
        ? LocalizationCatalog.Get("DashboardCancelRecommendationsEmpty")
        : string.Empty;

    public override async Task RefreshAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();

        IsBusy = true;
        try
        {
            Summary = await dashboardService.GetAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
