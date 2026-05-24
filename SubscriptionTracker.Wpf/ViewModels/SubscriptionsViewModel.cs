using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Wpf.Dialogs;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class SubscriptionsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppEventBus _eventBus;
    private readonly ISubscriptionEditorService _subscriptionEditorService;
    private readonly IImportPreviewDialogService _importPreviewDialogService;
    private readonly IImportSessionService _importSessionService;
    private readonly IImportRollbackService _importRollbackService;
    private readonly IDialogService _dialogService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ILocalizationService _localizationService;
    private SubscriptionListItemDto? _selectedItem;
    private ImportSessionHistoryItemViewModel? _selectedImportSession;
    private OptionItem<SubscriptionFilter>? _selectedFilter;
    private IReadOnlyList<OptionItem<SubscriptionFilter>> _filters = [];
    private string _searchText = string.Empty;

    public SubscriptionsViewModel(
        IServiceScopeFactory scopeFactory,
        AppEventBus eventBus,
        ISubscriptionEditorService subscriptionEditorService,
        IImportPreviewDialogService importPreviewDialogService,
        IImportSessionService importSessionService,
        IImportRollbackService importRollbackService,
        IDialogService dialogService,
        IAppSettingsService appSettingsService,
        ILocalizationService localizationService)
    {
        _scopeFactory = scopeFactory;
        _eventBus = eventBus;
        _subscriptionEditorService = subscriptionEditorService;
        _importPreviewDialogService = importPreviewDialogService;
        _importSessionService = importSessionService;
        _importRollbackService = importRollbackService;
        _dialogService = dialogService;
        _appSettingsService = appSettingsService;
        _localizationService = localizationService;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(EditAsync, () => SelectedItem is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedItem is not null);
        MarkPaidCommand = new AsyncRelayCommand(MarkPaidAsync, () => SelectedItem is not null);
        SkipCommand = new AsyncRelayCommand(SkipAsync, () => SelectedItem is not null);
        ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync, () => SelectedItem is not null);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        UndoLastImportCommand = new AsyncRelayCommand(UndoLastImportAsync);
        DownloadImportTemplateCommand = new AsyncRelayCommand(DownloadImportTemplateAsync);
        ViewImportSessionDetailsCommand = new AsyncRelayCommand(ViewImportSessionDetailsAsync, () => SelectedImportSession is not null);
        RollbackImportSessionCommand = new AsyncRelayCommand(RollbackSelectedImportSessionAsync, () => SelectedImportSession is not null);

        _localizationService.LanguageChanged += (_, _) =>
        {
            RebuildFilters();
            RebuildImportSessions(ImportSessions.Select(static item => item.Session));
        };
        RebuildFilters();
    }

    public ObservableCollection<SubscriptionListItemDto> Items { get; } = [];

    public ObservableCollection<ImportSessionHistoryItemViewModel> ImportSessions { get; } = [];

    public ICollectionView ItemsView { get; }

    public IReadOnlyList<OptionItem<SubscriptionFilter>> Filters
    {
        get => _filters;
        private set => SetProperty(ref _filters, value);
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

    public OptionItem<SubscriptionFilter>? SelectedFilter
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

    public string VisibleMonthlyTotalLabel => $"{VisibleMonthlyTotal:N2} {_appSettingsService.GetSettings().BaseCurrency}";

    public bool HasImportSessions => ImportSessions.Count > 0;

    public AsyncRelayCommand AddCommand { get; }

    public AsyncRelayCommand EditCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }

    public AsyncRelayCommand MarkPaidCommand { get; }

    public AsyncRelayCommand SkipCommand { get; }

    public AsyncRelayCommand ToggleActiveCommand { get; }

    public AsyncRelayCommand ExportCommand { get; }

    public AsyncRelayCommand ImportCommand { get; }

    public AsyncRelayCommand UndoLastImportCommand { get; }

    public AsyncRelayCommand DownloadImportTemplateCommand { get; }

    public AsyncRelayCommand ViewImportSessionDetailsCommand { get; }

    public AsyncRelayCommand RollbackImportSessionCommand { get; }

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

    public ImportSessionHistoryItemViewModel? SelectedImportSession
    {
        get => _selectedImportSession;
        set
        {
            if (SetProperty(ref _selectedImportSession, value))
            {
                ViewImportSessionDetailsCommand.NotifyCanExecuteChanged();
                RollbackImportSessionCommand.NotifyCanExecuteChanged();
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
            var importSessions = await _importSessionService.GetRecentAsync();
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            RebuildImportSessions(importSessions);

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

        var selectedFilter = SelectedFilter?.Value ?? SubscriptionFilter.All;
        var matchesFilter = selectedFilter switch
        {
            SubscriptionFilter.Active => subscription.IsActive,
            SubscriptionFilter.Disabled => !subscription.IsActive,
            SubscriptionFilter.DueSoon => subscription.DueSoon,
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

        if (!_dialogService.Confirm(
                LocalizationCatalog.Format("DeleteSubscriptionConfirmMessage", SelectedItem.Name),
                LocalizationCatalog.Get("DeleteSubscriptionConfirmTitle"),
                DialogKind.Warning,
                DialogButton.Delete,
                DialogButton.Cancel))
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
            Filter = LocalizationCatalog.Get("ExcelFileDialogFilter")
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
            _dialogService.ShowInfo(
                LocalizationCatalog.Get("ExportCompletedMessage"),
                LocalizationCatalog.Get("ExportCompletedTitle"));
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("ExportErrorTitle"));
        }
    }

    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = LocalizationCatalog.Get("ImportFileDialogFilter")
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<ISubscriptionImportService>();
            var preview = await importService.PreviewAsync(dialog.FileName);
            var selectedRowNumbers = await _importPreviewDialogService.ShowAsync(preview);
            if (selectedRowNumbers is null)
            {
                return;
            }

            var result = await importService.ImportAsync(dialog.FileName, selectedRowNumbers);

            await RefreshAsync();
            _eventBus.PublishDataChanged();

            var summary = LocalizationCatalog.Format(
                "ImportResultSummaryFormat",
                result.TotalRows,
                result.CreatedCount,
                result.UpdatedCount,
                result.CreatedCategoryCount,
                result.SkippedCount,
                result.IgnoredCount);

            if (result.Warnings.Count > 0)
            {
                var details = string.Join(Environment.NewLine, result.Warnings.Take(5));
                var moreSuffix = result.Warnings.Count > 5
                    ? Environment.NewLine + LocalizationCatalog.Format("ImportWarningsMoreFormat", result.Warnings.Count - 5)
                    : string.Empty;

                _dialogService.ShowWarning(
                    summary + Environment.NewLine + Environment.NewLine + details + moreSuffix,
                    LocalizationCatalog.Get("ImportCompletedWithWarningsTitle"));
            }
            else
            {
                _dialogService.ShowInfo(summary, LocalizationCatalog.Get("ImportCompletedTitle"));
            }
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("ImportFailedTitle"));
        }
    }

    private async Task DownloadImportTemplateAsync()
    {
        var dialog = new SaveFileDialog
        {
            FileName = LocalizationCatalog.Get("ImportTemplateDefaultFileName"),
            DefaultExt = ".xlsx",
            AddExtension = true,
            Filter = LocalizationCatalog.Get("ImportTemplateFileDialogFilter")
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ISubscriptionImportTemplateService>();
            await templateService.CreateTemplateAsync(dialog.FileName);

            _dialogService.ShowInfo(
                LocalizationCatalog.Format("ImportTemplateCompletedMessage", dialog.FileName),
                LocalizationCatalog.Get("ImportTemplateCompletedTitle"));
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("ImportTemplateFailedTitle"));
        }
    }

    private async Task UndoLastImportAsync()
    {
        try
        {
            var preview = await _importRollbackService.GetLastImportAsync();
            if (preview is null)
            {
                _dialogService.ShowInfo(
                    LocalizationCatalog.Get("ImportRollbackUnavailableMessage"),
                    LocalizationCatalog.Get("ImportRollbackUnavailableTitle"));
                return;
            }

            var shouldRollback = _dialogService.Confirm(
                LocalizationCatalog.Format(
                    "ImportRollbackConfirmMessage",
                    preview.SourceFileName,
                    preview.CreatedAtUtc.ToLocalTime(),
                    preview.AppliedRowsCount,
                    preview.CreatedCount,
                    preview.UpdatedCount,
                    preview.CreatedCategoryCount),
                LocalizationCatalog.Get("ImportRollbackConfirmTitle"),
                DialogKind.Warning,
                DialogButton.Restore,
                DialogButton.Cancel);

            if (!shouldRollback)
            {
                return;
            }

            var result = await _importRollbackService.RollbackLastImportAsync();
            await RefreshAsync();
            _eventBus.PublishDataChanged();

            _dialogService.ShowInfo(
                LocalizationCatalog.Format(
                    "ImportRollbackCompletedMessage",
                    result.SourceFileName,
                    result.DeletedSubscriptionsCount,
                    result.RestoredSubscriptionsCount,
                    result.DeletedCategoriesCount),
                LocalizationCatalog.Get("ImportRollbackCompletedTitle"));
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("ImportRollbackFailedTitle"));
        }
    }

    private async Task ViewImportSessionDetailsAsync()
    {
        if (SelectedImportSession is null)
        {
            return;
        }

        var details = await _importSessionService.GetDetailsAsync(SelectedImportSession.Session.Id);
        if (details is null)
        {
            _dialogService.ShowWarning(
                LocalizationCatalog.Get("ImportSessionNotFoundMessage"),
                LocalizationCatalog.Get("ImportSessionNotFoundTitle"));
            return;
        }

        var entries = details.Entries.Count == 0
            ? LocalizationCatalog.Get("ImportSessionDetailsNoEntries")
            : string.Join(Environment.NewLine, details.Entries.Select(static item => $"• {item.KindLabel}: {item.DisplayName}"));

        var rollbackLine = details.CanRollback
            ? LocalizationCatalog.Get("ImportHistoryRollbackAvailable")
            : details.RollbackBlockedReason ?? LocalizationCatalog.Get("ImportHistoryRollbackBlocked");

        var message = LocalizationCatalog.Format(
            "ImportSessionDetailsMessage",
            details.SourceFileName,
            details.CreatedAtUtc.ToLocalTime(),
            details.AppliedRowsCount,
            details.CreatedCount,
            details.UpdatedCount,
            details.CreatedCategoryCount,
            rollbackLine,
            entries);

        _dialogService.ShowInfo(
            message,
            LocalizationCatalog.Get("ImportSessionDetailsTitle"));
    }

    private async Task RollbackSelectedImportSessionAsync()
    {
        if (SelectedImportSession is null)
        {
            return;
        }

        try
        {
            var preview = await _importSessionService.GetDetailsAsync(SelectedImportSession.Session.Id);
            if (preview is null)
            {
                _dialogService.ShowWarning(
                    LocalizationCatalog.Get("ImportSessionNotFoundMessage"),
                    LocalizationCatalog.Get("ImportSessionNotFoundTitle"));
                return;
            }

            if (!preview.CanRollback)
            {
                _dialogService.ShowWarning(
                    preview.RollbackBlockedReason ?? LocalizationCatalog.Get("ImportHistoryRollbackBlocked"),
                    LocalizationCatalog.Get("ImportRollbackFailedTitle"));
                return;
            }

            var shouldRollback = _dialogService.Confirm(
                LocalizationCatalog.Format(
                    "ImportRollbackConfirmMessage",
                    preview.SourceFileName,
                    preview.CreatedAtUtc.ToLocalTime(),
                    preview.AppliedRowsCount,
                    preview.CreatedCount,
                    preview.UpdatedCount,
                    preview.CreatedCategoryCount),
                LocalizationCatalog.Get("ImportRollbackConfirmTitle"),
                DialogKind.Warning,
                DialogButton.Restore,
                DialogButton.Cancel);

            if (!shouldRollback)
            {
                return;
            }

            var result = await _importRollbackService.RollbackAsync(preview.Id);
            await RefreshAsync();
            _eventBus.PublishDataChanged();

            _dialogService.ShowInfo(
                LocalizationCatalog.Format(
                    "ImportRollbackCompletedMessage",
                    result.SourceFileName,
                    result.DeletedSubscriptionsCount,
                    result.RestoredSubscriptionsCount,
                    result.DeletedCategoriesCount),
                LocalizationCatalog.Get("ImportRollbackCompletedTitle"));
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("ImportRollbackFailedTitle"));
        }
    }

    private void RebuildFilters()
    {
        var selectedValue = SelectedFilter?.Value ?? SubscriptionFilter.All;
        Filters =
        [
            new OptionItem<SubscriptionFilter> { Value = SubscriptionFilter.All, Label = LocalizationCatalog.Get("FilterAll") },
            new OptionItem<SubscriptionFilter> { Value = SubscriptionFilter.Active, Label = LocalizationCatalog.Get("FilterActive") },
            new OptionItem<SubscriptionFilter> { Value = SubscriptionFilter.Disabled, Label = LocalizationCatalog.Get("FilterDisabled") },
            new OptionItem<SubscriptionFilter> { Value = SubscriptionFilter.DueSoon, Label = LocalizationCatalog.Get("FilterDueSoon") }
        ];

        SelectedFilter = Filters.FirstOrDefault(item => item.Value == selectedValue) ?? Filters.First();
        RaiseSummaryProperties();
    }

    private void RaiseSummaryProperties()
    {
        RaisePropertyChanged(nameof(VisibleCount));
        RaisePropertyChanged(nameof(ActiveCount));
        RaisePropertyChanged(nameof(VisibleMonthlyTotal));
        RaisePropertyChanged(nameof(VisibleMonthlyTotalLabel));
        RaisePropertyChanged(nameof(HasImportSessions));
    }

    private void RebuildImportSessions(IEnumerable<ImportSessionListItemDto> sessions)
    {
        ImportSessions.Clear();
        foreach (var session in sessions.Select((item, index) => new ImportSessionHistoryItemViewModel
                 {
                     Session = item,
                     IsLatest = index == 0
                 }))
        {
            ImportSessions.Add(session);
        }

        SelectedImportSession = ImportSessions.FirstOrDefault(item => item.IsLatest) ?? ImportSessions.FirstOrDefault();
        RaisePropertyChanged(nameof(HasImportSessions));
    }

    public enum SubscriptionFilter
    {
        All = 1,
        Active = 2,
        Disabled = 3,
        DueSoon = 4
    }
}
