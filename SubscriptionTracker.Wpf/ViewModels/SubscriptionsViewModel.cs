using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class SubscriptionsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppEventBus _eventBus;
    private readonly ISubscriptionEditorService _subscriptionEditorService;
    private readonly INotificationService _notificationService;
    private SubscriptionListItemDto? _selectedItem;
    private string _selectedFilter = "Все";
    private string _searchText = string.Empty;

    public SubscriptionsViewModel(
        IServiceScopeFactory scopeFactory,
        AppEventBus eventBus,
        ISubscriptionEditorService subscriptionEditorService,
        INotificationService notificationService)
    {
        _scopeFactory = scopeFactory;
        _eventBus = eventBus;
        _subscriptionEditorService = subscriptionEditorService;
        _notificationService = notificationService;

        Filters =
        [
            "Все",
            "Активные",
            "Отключенные",
            "Скоро спишутся"
        ];

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(EditAsync, () => SelectedItem is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedItem is not null);
        MarkPaidCommand = new AsyncRelayCommand(MarkPaidAsync, () => SelectedItem is not null);
        SkipCommand = new AsyncRelayCommand(SkipAsync, () => SelectedItem is not null);
        ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync, () => SelectedItem is not null);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
    }

    public ObservableCollection<SubscriptionListItemDto> Items { get; } = [];

    public ICollectionView ItemsView { get; }

    public IReadOnlyList<string> Filters { get; }

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

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                ItemsView.Refresh();
                RaiseSummaryProperties();
            }
        }
    }

    public int VisibleCount => ItemsView.Cast<object>().Count();

    public int ActiveCount => Items.Count(static item => item.IsActive);

    public decimal VisibleMonthlyTotal => ItemsView.Cast<SubscriptionListItemDto>().Sum(static item => item.MonthlyCostInBaseCurrency);

    public string VisibleMonthlyTotalLabel => $"{VisibleMonthlyTotal:N2} RUB";

    public AsyncRelayCommand AddCommand { get; }

    public AsyncRelayCommand EditCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }

    public AsyncRelayCommand MarkPaidCommand { get; }

    public AsyncRelayCommand SkipCommand { get; }

    public AsyncRelayCommand ToggleActiveCommand { get; }

    public AsyncRelayCommand ExportCommand { get; }

    public SubscriptionListItemDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                EditCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                MarkPaidCommand.NotifyCanExecuteChanged();
                SkipCommand.NotifyCanExecuteChanged();
                ToggleActiveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public override async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        IsBusy = true;
        try
        {
            var items = await subscriptionService.GetAllAsync();
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
        if (item is not SubscriptionListItemDto subscription)
        {
            return false;
        }

        var matchesFilter = SelectedFilter switch
        {
            "Активные" => subscription.IsActive,
            "Отключенные" => !subscription.IsActive,
            "Скоро спишутся" => subscription.DueSoon,
            _ => true
        };

        var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
            || subscription.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || subscription.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || subscription.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesFilter && matchesSearch;
    }

    private async Task AddAsync()
    {
        if (await _subscriptionEditorService.ShowAsync(null))
        {
            await RefreshAsync();
            _eventBus.PublishDataChanged();
        }
    }

    private async Task EditAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        if (await _subscriptionEditorService.ShowAsync(SelectedItem))
        {
            await RefreshAsync();
            _eventBus.PublishDataChanged();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        await service.DeleteAsync(SelectedItem.Id);
        await RefreshAsync();
        _eventBus.PublishDataChanged();
    }

    private async Task MarkPaidAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        await service.MarkAsPaidAsync(SelectedItem.Id);
        await RefreshAsync();
        _eventBus.PublishDataChanged();
    }

    private async Task SkipAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        await service.SkipNextPaymentAsync(SelectedItem.Id);
        await RefreshAsync();
        _eventBus.PublishDataChanged();
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        await service.ToggleActiveAsync(SelectedItem.Id);
        await RefreshAsync();
        _eventBus.PublishDataChanged();
    }

    private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"subscription-tracker-{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
            Filter = "Excel workbook (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();
            await exportService.ExportAsync(dialog.FileName);
            _notificationService.ShowInfo("Excel-файл успешно сохранен.", "Экспорт завершен");
        }
        catch (Exception exception)
        {
            _notificationService.ShowError(exception.Message, "Ошибка экспорта");
        }
    }

    private void RaiseSummaryProperties()
    {
        RaisePropertyChanged(nameof(VisibleCount));
        RaisePropertyChanged(nameof(ActiveCount));
        RaisePropertyChanged(nameof(VisibleMonthlyTotal));
        RaisePropertyChanged(nameof(VisibleMonthlyTotalLabel));
    }
}
