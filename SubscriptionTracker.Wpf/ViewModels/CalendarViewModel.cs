using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class CalendarViewModel(IServiceScopeFactory scopeFactory) : ViewModelBase
{
    private IReadOnlyList<CalendarMonthDto> _months = Array.Empty<CalendarMonthDto>();

    public IReadOnlyList<CalendarMonthDto> Months
    {
        get => _months;
        private set => SetProperty(ref _months, value);
    }

    public override async Task RefreshAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        IsBusy = true;
        try
        {
            Months = await subscriptionService.GetCalendarAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
