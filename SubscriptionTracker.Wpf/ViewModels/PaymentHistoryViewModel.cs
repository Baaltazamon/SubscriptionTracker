using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class PaymentHistoryViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILocalizationService _localizationService;
    private string _searchText = string.Empty;
    private OptionItem<HistoryFilter>? _selectedStatusFilter;
    private IReadOnlyList<OptionItem<HistoryFilter>> _statusFilters = [];

    public PaymentHistoryViewModel(IServiceScopeFactory scopeFactory, ILocalizationService localizationService)
    {
        _scopeFactory = scopeFactory;
        _localizationService = localizationService;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        _localizationService.LanguageChanged += (_, _) => RebuildFilters();
        RebuildFilters();
    }

    public ObservableCollection<PaymentHistoryDto> Items { get; } = [];

    public ICollectionView ItemsView { get; }

    public IReadOnlyList<OptionItem<HistoryFilter>> StatusFilters
    {
        get => _statusFilters;
        private set => SetProperty(ref _statusFilters, value);
    }

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

    public OptionItem<HistoryFilter>? SelectedStatusFilter
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

        var matchesStatus = (SelectedStatusFilter?.Value ?? HistoryFilter.All) switch
        {
            HistoryFilter.Paid => historyItem.Status == PaymentStatus.Paid,
            HistoryFilter.Planned => historyItem.Status == PaymentStatus.Planned,
            HistoryFilter.Skipped => historyItem.Status == PaymentStatus.Skipped,
            HistoryFilter.Cancelled => historyItem.Status == PaymentStatus.Cancelled,
            HistoryFilter.Failed => historyItem.Status == PaymentStatus.Failed,
            _ => true
        };

        var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
            || historyItem.SubscriptionName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || historyItem.NoteLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || historyItem.AmountLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesStatus && matchesSearch;
    }

    private void RebuildFilters()
    {
        var selectedValue = SelectedStatusFilter?.Value ?? HistoryFilter.All;
        StatusFilters =
        [
            new OptionItem<HistoryFilter> { Value = HistoryFilter.All, Label = LocalizationCatalog.Get("FilterAll") },
            new OptionItem<HistoryFilter> { Value = HistoryFilter.Paid, Label = LocalizationCatalog.Get("PaymentStatusPaid") },
            new OptionItem<HistoryFilter> { Value = HistoryFilter.Planned, Label = LocalizationCatalog.Get("PaymentStatusPlanned") },
            new OptionItem<HistoryFilter> { Value = HistoryFilter.Skipped, Label = LocalizationCatalog.Get("PaymentStatusSkipped") },
            new OptionItem<HistoryFilter> { Value = HistoryFilter.Cancelled, Label = LocalizationCatalog.Get("PaymentStatusCancelled") },
            new OptionItem<HistoryFilter> { Value = HistoryFilter.Failed, Label = LocalizationCatalog.Get("PaymentStatusFailed") }
        ];

        SelectedStatusFilter = StatusFilters.FirstOrDefault(item => item.Value == selectedValue) ?? StatusFilters.First();
        RaiseSummaryProperties();
    }

    private void RaiseSummaryProperties()
    {
        RaisePropertyChanged(nameof(VisibleCount));
        RaisePropertyChanged(nameof(PaidCount));
        RaisePropertyChanged(nameof(PlannedCount));
    }

    public enum HistoryFilter
    {
        All = 1,
        Paid = 2,
        Planned = 3,
        Skipped = 4,
        Cancelled = 5,
        Failed = 6
    }
}
