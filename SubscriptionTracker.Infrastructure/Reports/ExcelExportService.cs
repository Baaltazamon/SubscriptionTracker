using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Application.Services;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure.Reports;

public sealed class ExcelExportService(AppDbContext dbContext) : IExportService
{
    public async Task ExportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(static subscription => subscription.Category)
            .OrderBy(static subscription => subscription.Name)
            .ToListAsync(cancellationToken);

        var payments = await dbContext.PaymentHistories
            .AsNoTracking()
            .Include(static payment => payment.Subscription)
            .OrderByDescending(static payment => payment.PaymentDate)
            .ToListAsync(cancellationToken);

        using var package = new ExcelPackage();
        var subscriptionsSheet = package.Workbook.Worksheets.Add(LocalizationCatalog.Get("ExcelSheetSubscriptions"));
        var historySheet = package.Workbook.Worksheets.Add(LocalizationCatalog.Get("ExcelSheetHistory"));

        subscriptionsSheet.Cells[1, 1].Value = LocalizationCatalog.Get("ExcelHeaderName");
        subscriptionsSheet.Cells[1, 2].Value = LocalizationCatalog.Get("ExcelHeaderCategory");
        subscriptionsSheet.Cells[1, 3].Value = LocalizationCatalog.Get("ExcelHeaderAmount");
        subscriptionsSheet.Cells[1, 4].Value = LocalizationCatalog.Get("ExcelHeaderCurrency");
        subscriptionsSheet.Cells[1, 5].Value = LocalizationCatalog.Get("ExcelHeaderCycle");
        subscriptionsSheet.Cells[1, 6].Value = LocalizationCatalog.Get("ExcelHeaderNextPayment");
        subscriptionsSheet.Cells[1, 7].Value = LocalizationCatalog.Get("ExcelHeaderStatus");

        for (var index = 0; index < subscriptions.Count; index++)
        {
            var row = index + 2;
            var item = subscriptions[index];
            subscriptionsSheet.Cells[row, 1].Value = item.Name;
            subscriptionsSheet.Cells[row, 2].Value = item.Category.Name;
            subscriptionsSheet.Cells[row, 3].Value = item.Amount;
            subscriptionsSheet.Cells[row, 4].Value = item.Currency;
            subscriptionsSheet.Cells[row, 5].Value = BillingCycleDisplayFormatter.ToLabel(item.BillingCycle);
            subscriptionsSheet.Cells[row, 6].Value = item.NextPaymentDate.ToString("d");
            subscriptionsSheet.Cells[row, 7].Value = item.IsActive
                ? LocalizationCatalog.Get("ExcelStatusActive")
                : LocalizationCatalog.Get("ExcelStatusDisabled");
        }

        historySheet.Cells[1, 1].Value = LocalizationCatalog.Get("ExcelHeaderDate");
        historySheet.Cells[1, 2].Value = LocalizationCatalog.Get("ExcelHeaderSubscription");
        historySheet.Cells[1, 3].Value = LocalizationCatalog.Get("ExcelHeaderAmount");
        historySheet.Cells[1, 4].Value = LocalizationCatalog.Get("ExcelHeaderCurrency");
        historySheet.Cells[1, 5].Value = LocalizationCatalog.Get("ExcelHeaderStatus");
        historySheet.Cells[1, 6].Value = LocalizationCatalog.Get("ExcelHeaderComment");

        for (var index = 0; index < payments.Count; index++)
        {
            var row = index + 2;
            var item = payments[index];
            historySheet.Cells[row, 1].Value = item.PaymentDate.ToString("d");
            historySheet.Cells[row, 2].Value = item.Subscription.Name;
            historySheet.Cells[row, 3].Value = item.Amount;
            historySheet.Cells[row, 4].Value = item.Currency;
            historySheet.Cells[row, 5].Value = BillingCycleDisplayFormatter.ToLabel(item.Status);
            historySheet.Cells[row, 6].Value = item.Note;
        }

        subscriptionsSheet.Cells.AutoFitColumns();
        historySheet.Cells.AutoFitColumns();

        await package.SaveAsAsync(new FileInfo(filePath), cancellationToken);
    }
}
