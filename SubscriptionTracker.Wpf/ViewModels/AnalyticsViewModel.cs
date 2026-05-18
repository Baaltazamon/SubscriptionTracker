using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class AnalyticsViewModel(IServiceScopeFactory scopeFactory) : ViewModelBase
{
    private DashboardSummaryDto _summary = new();
    private decimal _potentialSavings;

    public DashboardSummaryDto Summary
    {
        get => _summary;
        private set
        {
            if (SetProperty(ref _summary, value))
            {
                RaisePropertyChanged(nameof(PotentialSavingsLabel));
            }
        }
    }

    public string PotentialSavingsLabel => $"{_potentialSavings:N2} {Summary.BaseCurrency}";

    public override async Task RefreshAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        IsBusy = true;
        try
        {
            Summary = await dashboardService.GetAsync();
            var subscriptions = await subscriptionService.GetAllAsync();
            _potentialSavings = subscriptions
                .Where(static item => item.IsActive)
                .OrderByDescending(static item => item.MonthlyCostInBaseCurrency)
                .Take(3)
                .Sum(static item => item.MonthlyCostInBaseCurrency);
            RaisePropertyChanged(nameof(PotentialSavingsLabel));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
