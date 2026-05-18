using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SubscriptionTracker.Application.Interfaces;
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
        var subscriptionsSheet = package.Workbook.Worksheets.Add("Подписки");
        var historySheet = package.Workbook.Worksheets.Add("История");

        subscriptionsSheet.Cells[1, 1].Value = "Название";
        subscriptionsSheet.Cells[1, 2].Value = "Категория";
        subscriptionsSheet.Cells[1, 3].Value = "Сумма";
        subscriptionsSheet.Cells[1, 4].Value = "Валюта";
        subscriptionsSheet.Cells[1, 5].Value = "Период";
        subscriptionsSheet.Cells[1, 6].Value = "Следующее списание";
        subscriptionsSheet.Cells[1, 7].Value = "Статус";

        for (var index = 0; index < subscriptions.Count; index++)
        {
            var row = index + 2;
            var item = subscriptions[index];
            subscriptionsSheet.Cells[row, 1].Value = item.Name;
            subscriptionsSheet.Cells[row, 2].Value = item.Category.Name;
            subscriptionsSheet.Cells[row, 3].Value = item.Amount;
            subscriptionsSheet.Cells[row, 4].Value = item.Currency;
            subscriptionsSheet.Cells[row, 5].Value = item.BillingCycle.ToString();
            subscriptionsSheet.Cells[row, 6].Value = item.NextPaymentDate.ToString("dd.MM.yyyy");
            subscriptionsSheet.Cells[row, 7].Value = item.IsActive ? "Активна" : "Отключена";
        }

        historySheet.Cells[1, 1].Value = "Дата";
        historySheet.Cells[1, 2].Value = "Подписка";
        historySheet.Cells[1, 3].Value = "Сумма";
        historySheet.Cells[1, 4].Value = "Валюта";
        historySheet.Cells[1, 5].Value = "Статус";
        historySheet.Cells[1, 6].Value = "Комментарий";

        for (var index = 0; index < payments.Count; index++)
        {
            var row = index + 2;
            var item = payments[index];
            historySheet.Cells[row, 1].Value = item.PaymentDate.ToString("dd.MM.yyyy");
            historySheet.Cells[row, 2].Value = item.Subscription.Name;
            historySheet.Cells[row, 3].Value = item.Amount;
            historySheet.Cells[row, 4].Value = item.Currency;
            historySheet.Cells[row, 5].Value = item.Status.ToString();
            historySheet.Cells[row, 6].Value = item.Note;
        }

        subscriptionsSheet.Cells.AutoFitColumns();
        historySheet.Cells.AutoFitColumns();

        await package.SaveAsAsync(new FileInfo(filePath), cancellationToken);
    }
}
