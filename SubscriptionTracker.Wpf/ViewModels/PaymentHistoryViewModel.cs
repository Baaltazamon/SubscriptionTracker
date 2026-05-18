using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Domain.Enums;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class PaymentHistoryViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private string _searchText = string.Empty;
    private string _selectedStatusFilter = "Все";

    public PaymentHistoryViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        StatusFilters =
        [
            "Все",
            "Оплачен",
            "Запланирован",
            "Пропущен",
            "Отменен",
            "Ошибка"
        ];

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;
    }

    public ObservableCollection<PaymentHistoryDto> Items { get; } = [];

    public ICollectionView ItemsView { get; }

    public IReadOnlyList<string> StatusFilters { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ItemsView.Refresh();
                RaiseSummaryProperties();
            }
        }
    }

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                ItemsView.Refresh();
                RaiseSummaryProperties();
            }
        }
    }

    public int VisibleCount => ItemsView.Cast<object>().Count();

    public int PaidCount => Items.Count(static item => item.Status == PaymentStatus.Paid);

    public int PlannedCount => Items.Count(static item => item.Status == PaymentStatus.Planned);

    public override async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var paymentHistoryService = scope.ServiceProvider.GetRequiredService<IPaymentHistoryService>();

        IsBusy = true;
        try
        {
            var items = await paymentHistoryService.GetAllAsync();
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            ItemsView.Refresh();
            RaiseSummaryProperties();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool FilterItem(object item)
    {
        if (item is not PaymentHistoryDto historyItem)
        {
            return false;
        }

        var matchesStatus = SelectedStatusFilter switch
        {
            "Оплачен" => historyItem.Status == PaymentStatus.Paid,
            "Запланирован" => historyItem.Status == PaymentStatus.Planned,
            "Пропущен" => historyItem.Status == PaymentStatus.Skipped,
            "Отменен" => historyItem.Status == PaymentStatus.Cancelled,
            "Ошибка" => historyItem.Status == PaymentStatus.Failed,
            _ => true
        };

        var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
            || historyItem.SubscriptionName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || historyItem.NoteLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || historyItem.AmountLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesStatus && matchesSearch;
    }

    private void RaiseSummaryProperties()
    {
        RaisePropertyChanged(nameof(VisibleCount));
        RaisePropertyChanged(nameof(PaidCount));
        RaisePropertyChanged(nameof(PlannedCount));
    }
}
