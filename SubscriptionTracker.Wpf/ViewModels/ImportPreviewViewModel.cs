using System.Collections.ObjectModel;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class ImportPreviewViewModel
{
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
        CanImport = preview.CanImport;
    }

    public ObservableCollection<ImportPreviewItemViewModel> Items { get; }

    public ObservableCollection<string> Warnings { get; }

    public int TotalRows { get; }

    public int CreatedCount { get; }

    public int UpdatedCount { get; }

    public int CreatedCategoryCount { get; }

    public int SkippedCount { get; }

    public bool CanImport { get; }

    public string WindowTitle => LocalizationCatalog.Get("ImportPreviewWindowTitle");

    public string SummaryText => LocalizationCatalog.Format(
        "ImportSummaryFormat",
        TotalRows,
        CreatedCount,
        UpdatedCount,
        CreatedCategoryCount,
        SkippedCount);

    public bool HasWarnings => Warnings.Count > 0;
}
