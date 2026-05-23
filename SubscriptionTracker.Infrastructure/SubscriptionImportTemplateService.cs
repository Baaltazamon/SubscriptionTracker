using OfficeOpenXml;
using OfficeOpenXml.Style;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Infrastructure;

public sealed class SubscriptionImportTemplateService : ISubscriptionImportTemplateService
{
    private static readonly string[] TemplateHeaders =
    [
        "Name",
        "Category",
        "Amount",
        "Currency",
        "Cycle",
        "Next payment",
        "Description",
        "First payment",
        "Status",
        "IsActive",
        "AutoRenewal",
        "ReminderDays",
        "IsLowUsage"
    ];

    private static readonly string[][] SampleRows =
    [
        ["ChatGPT Plus", "AI tools", "20", "USD", "Monthly", "2026-06-17", "Personal AI assistant", "2026-05-17", "Active", "true", "true", "3", "false"],
        ["Spotify Family", "Streaming", "10.99", "USD", "Monthly", "2026-06-21", "Family music plan", "2026-05-21", "Active", "true", "true", "2", "false"],
        ["Domain renewal", "Domains", "1200", "RUB", "Yearly", "2027-03-01", "Main domain", "2026-03-01", "Active", "true", "true", "14", "true"]
    ];

    public async Task CreateTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath);

        if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            await CreateCsvTemplateAsync(filePath, cancellationToken);
            return;
        }

        await CreateExcelTemplateAsync(filePath, cancellationToken);
    }

    private static async Task CreateCsvTemplateAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = new List<string>
        {
            string.Join(';', TemplateHeaders.Select(EscapeCsvValue))
        };

        lines.AddRange(SampleRows.Select(row => string.Join(';', row.Select(EscapeCsvValue))));

        await File.WriteAllLinesAsync(filePath, lines, cancellationToken);
    }

    private static async Task CreateExcelTemplateAsync(string filePath, CancellationToken cancellationToken)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var dataSheet = package.Workbook.Worksheets.Add(LocalizationCatalog.Get("ImportTemplateSheetName"));
        var notesSheet = package.Workbook.Worksheets.Add(LocalizationCatalog.Get("ImportTemplateNotesSheetName"));

        for (var column = 0; column < TemplateHeaders.Length; column++)
        {
            dataSheet.Cells[1, column + 1].Value = TemplateHeaders[column];
        }

        using (var headerRange = dataSheet.Cells[1, 1, 1, TemplateHeaders.Length])
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 244, 114, 33));
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        for (var row = 0; row < SampleRows.Length; row++)
        {
            for (var column = 0; column < SampleRows[row].Length; column++)
            {
                dataSheet.Cells[row + 2, column + 1].Value = SampleRows[row][column];
            }
        }

        dataSheet.Cells.AutoFitColumns();
        dataSheet.View.FreezePanes(2, 1);

        notesSheet.Cells[1, 1].Value = LocalizationCatalog.Get("ImportTemplateNotesTitle");
        notesSheet.Cells[1, 1].Style.Font.Bold = true;
        notesSheet.Cells[2, 1].Value = LocalizationCatalog.Get("ImportTemplateNotesSummary");
        notesSheet.Cells[4, 1].Value = LocalizationCatalog.Get("ImportTemplateRequiredColumns");
        notesSheet.Cells[5, 1].Value = "Name, Category, Amount, Cycle, Next payment";
        notesSheet.Cells[7, 1].Value = LocalizationCatalog.Get("ImportTemplateOptionalColumns");
        notesSheet.Cells[8, 1].Value = "Currency, Description, First payment, Status, IsActive, AutoRenewal, ReminderDays, IsLowUsage";
        notesSheet.Cells[10, 1].Value = LocalizationCatalog.Get("ImportTemplateCycleExamples");
        notesSheet.Cells[11, 1].Value = "Monthly, Quarterly, SemiAnnual, Yearly";
        notesSheet.Cells[13, 1].Value = LocalizationCatalog.Get("ImportTemplateBooleanExamples");
        notesSheet.Cells[14, 1].Value = "true / false, yes / no, 1 / 0";
        notesSheet.Cells[16, 1].Value = LocalizationCatalog.Get("ImportTemplateDateExample");
        notesSheet.Cells[17, 1].Value = "2026-06-17";
        notesSheet.Cells.AutoFitColumns();

        await package.SaveAsAsync(new FileInfo(filePath), cancellationToken);
    }

    private static string EscapeCsvValue(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
