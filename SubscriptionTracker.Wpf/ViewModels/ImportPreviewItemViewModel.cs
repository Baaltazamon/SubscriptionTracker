using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Application.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class ImportPreviewItemViewModel
{
    public ImportPreviewItemViewModel(ImportPreviewItemDto item)
    {
        RowNumber = item.RowNumber;
        Name = string.IsNullOrWhiteSpace(item.Name)
            ? LocalizationCatalog.Format("ImportPreviewFallbackName", item.RowNumber)
            : item.Name;
        CategoryName = item.CategoryName;
        AmountLabel = item.Amount == 0m
            ? "—"
            : $"{item.Amount:N2} {item.Currency}";
        CycleLabel = item.Amount == 0m
            ? "—"
            : BillingCycleDisplayFormatter.ToLabel(item.BillingCycle);
        NextPaymentLabel = item.NextPaymentDate == default
            ? "—"
            : item.NextPaymentDate.ToString("dd.MM.yyyy");
        ActionLabel = item.Action switch
        {
            ImportPreviewAction.Create => LocalizationCatalog.Get("ImportPreviewActionCreate"),
            ImportPreviewAction.Update => LocalizationCatalog.Get("ImportPreviewActionUpdate"),
            _ => LocalizationCatalog.Get("ImportPreviewActionSkip")
        };
        ActionColorHex = item.Action switch
        {
            ImportPreviewAction.Create => "#1D4ED8",
            ImportPreviewAction.Update => "#F97316",
            _ => "#64748B"
        };

        Note = item.WillCreateCategory
            ? string.IsNullOrWhiteSpace(item.Note)
                ? LocalizationCatalog.Get("ImportPreviewNewCategoryNote")
                : item.Note
            : item.Note;
    }

    public int RowNumber { get; }

    public string Name { get; }

    public string CategoryName { get; }

    public string AmountLabel { get; }

    public string CycleLabel { get; }

    public string NextPaymentLabel { get; }

    public string ActionLabel { get; }

    public string ActionColorHex { get; }

    public string? Note { get; }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}
