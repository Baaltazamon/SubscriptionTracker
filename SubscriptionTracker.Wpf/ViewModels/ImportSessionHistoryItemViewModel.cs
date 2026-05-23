using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class ImportSessionHistoryItemViewModel
{
    public required ImportSessionListItemDto Session { get; init; }

    public bool IsLatest { get; init; }

    public string SourceFileName => Session.SourceFileName;

    public string CreatedAtLabel => Session.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public string RowsLabel => LocalizationCatalog.Format("ImportHistoryRowsFormat", Session.AppliedRowsCount);

    public string SummaryLabel => LocalizationCatalog.Format(
        "ImportHistoryCountsFormat",
        Session.CreatedCount,
        Session.UpdatedCount,
        Session.CreatedCategoryCount);
}
