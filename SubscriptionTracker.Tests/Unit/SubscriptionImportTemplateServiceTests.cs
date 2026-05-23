using OfficeOpenXml;
using SubscriptionTracker.Infrastructure;

namespace SubscriptionTracker.Tests.Unit;

public sealed class SubscriptionImportTemplateServiceTests
{
    [Fact]
    public async Task CreateTemplateAsync_Xlsx_CreatesWorkbookWithTemplateAndReferenceSheets()
    {
        var path = CreateTempFile(".xlsx");

        try
        {
            var service = new SubscriptionImportTemplateService();

            await service.CreateTemplateAsync(path);

            Assert.True(File.Exists(path));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(new FileInfo(path));
            Assert.Equal(2, package.Workbook.Worksheets.Count);
            Assert.Equal("Name", package.Workbook.Worksheets[0].Cells[1, 1].Text);
            Assert.Equal("Category", package.Workbook.Worksheets[0].Cells[1, 2].Text);
            Assert.Equal("ChatGPT Plus", package.Workbook.Worksheets[0].Cells[2, 1].Text);
        }
        finally
        {
            CleanupTempFile(path);
        }
    }

    [Fact]
    public async Task CreateTemplateAsync_Csv_CreatesCsvWithHeaderAndSampleRows()
    {
        var path = CreateTempFile(".csv");

        try
        {
            var service = new SubscriptionImportTemplateService();

            await service.CreateTemplateAsync(path);

            Assert.True(File.Exists(path));

            var lines = await File.ReadAllLinesAsync(path);
            Assert.True(lines.Length >= 4);
            Assert.StartsWith("Name;Category;Amount;Currency;Cycle;Next payment", lines[0]);
            Assert.Contains("ChatGPT Plus", lines[1]);
        }
        finally
        {
            CleanupTempFile(path);
        }
    }

    private static string CreateTempFile(string extension)
    {
        var root = Path.Combine(Path.GetTempPath(), "SubscriptionTracker.Tests", "SubscriptionImportTemplateServiceTests");
        Directory.CreateDirectory(root);
        return Path.Combine(root, Guid.NewGuid().ToString("N") + extension);
    }

    private static void CleanupTempFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
