using System.Collections.ObjectModel;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class ImportPreviewViewModel : ViewModelBase
{
    private bool _selectAllImportable = true;

    public ImportPreviewViewModel(ImportSubscriptionsPreviewDto preview)
    {
        Items = new ObservableCollection<ImportPreviewItemViewModel>(
            preview.Items.Select(static item => new ImportPreviewItemViewModel(item)));

        Warnings = new ObservableCollection<string>(preview.Warnings);
        TotalRows = preview.TotalRows;
        CreatedCount = preview.CreatedCount;
        UpdatedCount = preview.UpdatedCount;
        CreatedCategoryCount = preview.CreatedCategoryCount;
        SkippedCount = preview.SkippedCount;
        foreach (var item in Items)
        {
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ImportPreviewItemViewModel.IsSelected))
                {
                    SyncSelectionState();
                }
            };
        }

        SyncSelectionState();
    }

    public ObservableCollection<ImportPreviewItemViewModel> Items { get; }

    public ObservableCollection<string> Warnings { get; }

    public int TotalRows { get; }

    public int CreatedCount { get; }

    public int UpdatedCount { get; }

    public int CreatedCategoryCount { get; }

    public int SkippedCount { get; }

    public bool SelectAllImportable
    {
        get => _selectAllImportable;
        set
        {
            if (!SetProperty(ref _selectAllImportable, value))
            {
                return;
            }

            foreach (var item in Items.Where(static item => item.IsSelectable))
            {
                item.IsSelected = value;
            }

            RaisePropertyChanged(nameof(SelectedCount));
            RaisePropertyChanged(nameof(CanImport));
            RaisePropertyChanged(nameof(SelectedSummaryText));
        }
    }

    public string WindowTitle => LocalizationCatalog.Get("ImportPreviewWindowTitle");

    public string SummaryText => LocalizationCatalog.Format(
        "ImportPreviewSummaryFormat",
        TotalRows,
        CreatedCount,
        UpdatedCount,
        CreatedCategoryCount,
        SkippedCount);

    public bool HasWarnings => Warnings.Count > 0;

    public int SelectedCount => Items.Count(static item => item.IsSelectable && item.IsSelected);

    public bool CanImport => SelectedCount > 0;

    public string SelectedSummaryText => LocalizationCatalog.Format(
        "ImportPreviewSelectedFormat",
        SelectedCount,
        Items.Count(static item => item.IsSelectable));

    public IReadOnlyList<int> SelectedRowNumbers => Items
        .Where(static item => item.IsSelectable && item.IsSelected)
        .Select(static item => item.RowNumber)
        .ToArray();

    private void SyncSelectionState()
    {
        var importableItems = Items.Where(static item => item.IsSelectable).ToArray();
        var allSelected = importableItems.Length > 0 && importableItems.All(static item => item.IsSelected);

        if (_selectAllImportable != allSelected)
        {
            _selectAllImportable = allSelected;
            RaisePropertyChanged(nameof(SelectAllImportable));
        }

        RaisePropertyChanged(nameof(SelectedCount));
        RaisePropertyChanged(nameof(CanImport));
        RaisePropertyChanged(nameof(SelectedSummaryText));
    }
}
