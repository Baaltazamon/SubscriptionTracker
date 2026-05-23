using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Entities;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure;

public sealed class SubscriptionImportService(
    AppDbContext dbContext,
    ISubscriptionService subscriptionService) : ISubscriptionImportService
{
    private static readonly string[] SupportedExtensions = [".xlsx", ".csv"];
    private const string DefaultCategoryColor = "#94A3B8";

    public async Task<ImportSubscriptionsResultDto> ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("ImportUnsupportedFormat"));
        }

        var rows = string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
            ? await ReadExcelRowsAsync(filePath, cancellationToken)
            : await ReadCsvRowsAsync(filePath, cancellationToken);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("ImportNoData"));
        }

        var categoriesByName = await dbContext.Categories
            .ToDictionaryAsync(
                static item => NormalizeKey(item.Name),
                cancellationToken);

        var subscriptionsByName = await dbContext.Subscriptions
            .AsNoTracking()
            .ToDictionaryAsync(
                static item => NormalizeKey(item.Name),
                cancellationToken);

        var warnings = new List<string>();
        var createdCount = 0;
        var updatedCount = 0;
        var createdCategoryCount = 0;
        var skippedCount = 0;

        foreach (var row in rows)
        {
            try
            {
                var category = await GetOrCreateCategoryAsync(row, categoriesByName, cancellationToken);
                if (category.WasCreated)
                {
                    createdCategoryCount++;
                }

                var name = row.GetRequired("name", "subscription");
                var key = NormalizeKey(name);
                subscriptionsByName.TryGetValue(key, out var existing);

                var nextPaymentDate = row.GetRequiredDate(
                    "nextpayment",
                    "nextpaymentdate",
                    "nextcharge",
                    "next");

                var firstPaymentDate = row.GetOptionalDate(
                    "firstpayment",
                    "firstpaymentdate",
                    "first")
                    ?? nextPaymentDate;

                var request = new SaveSubscriptionRequest
                {
                    Id = existing?.Id,
                    Name = name.Trim(),
                    Description = row.GetOptional("description"),
                    CategoryId = category.Category.Id,
                    Amount = row.GetRequiredDecimal("amount", "sum"),
                    Currency = row.GetOptional("currency")?.Trim().ToUpperInvariant() ?? "RUB",
                    BillingCycle = row.GetRequiredBillingCycle("cycle", "billingcycle", "period"),
                    FirstPaymentDate = firstPaymentDate,
                    NextPaymentDate = nextPaymentDate,
                    IsActive = row.GetOptionalBoolean("isactive")
                        ?? row.GetOptionalStatus("status")
                        ?? true,
                    AutoRenewal = row.GetOptionalBoolean("autorenewal", "auto") ?? true,
                    ReminderDaysBefore = row.GetOptionalInt("reminderdays", "reminderdaysbefore", "reminder") ?? 3,
                    IsLowUsage = row.GetOptionalBoolean("islowusage", "lowusage") ?? false
                };

                var saved = await subscriptionService.SaveAsync(request, cancellationToken);

                if (existing is null)
                {
                    createdCount++;
                }
                else
                {
                    updatedCount++;
                }

                subscriptionsByName[key] = new Subscription
                {
                    Id = saved.Id,
                    Name = saved.Name
                };
            }
            catch (Exception exception)
            {
                skippedCount++;
                warnings.Add(LocalizationCatalog.Format("ImportWarningRowFormat", row.RowNumber, exception.Message));
            }
        }

        return new ImportSubscriptionsResultDto
        {
            TotalRows = rows.Count,
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            CreatedCategoryCount = createdCategoryCount,
            SkippedCount = skippedCount,
            Warnings = warnings
        };
    }

    private async Task<(Category Category, bool WasCreated)> GetOrCreateCategoryAsync(
        ImportRow row,
        IDictionary<string, Category> categoriesByName,
        CancellationToken cancellationToken)
    {
        var categoryName = row.GetRequired("category");
        var key = NormalizeKey(categoryName);

        if (categoriesByName.TryGetValue(key, out var existing))
        {
            return (existing, false);
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryName.Trim(),
            ColorHex = DefaultCategoryColor,
            IsSystem = false
        };

        await dbContext.Categories.AddAsync(category, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        categoriesByName[key] = category;
        return (category, true);
    }

    private static Task<IReadOnlyList<ImportRow>> ReadExcelRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException(LocalizationCatalog.Get("ImportNoWorksheet"));

        var dimension = worksheet.Dimension;
        if (dimension is null || dimension.End.Row < 2)
        {
            return Task.FromResult<IReadOnlyList<ImportRow>>([]);
        }

        var headerMap = BuildHeaderMap(
            Enumerable.Range(1, dimension.End.Column)
                .ToDictionary(
                    column => column,
                    column => worksheet.Cells[1, column].Text));

        var rows = new List<ImportRow>();
        for (var rowIndex = 2; rowIndex <= dimension.End.Row; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in headerMap)
            {
                values[pair.Value] = worksheet.Cells[rowIndex, pair.Key].Text?.Trim() ?? string.Empty;
            }

            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ImportRow(rowIndex, values));
        }

        return Task.FromResult<IReadOnlyList<ImportRow>>(rows);
    }

    private static async Task<IReadOnlyList<ImportRow>> ReadCsvRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length <= 1)
        {
            return [];
        }

        var separator = DetectSeparator(lines[0]);
        var headerCells = ParseCsvLine(lines[0], separator);
        var headerMap = BuildHeaderMap(
            headerCells
                .Select((header, index) => new { Header = header, Column = index })
                .ToDictionary(static item => item.Column, static item => item.Header));

        var rows = new List<ImportRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = ParseCsvLine(lines[lineIndex], separator);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in headerMap)
            {
                values[pair.Value] = pair.Key < cells.Count ? cells[pair.Key].Trim() : string.Empty;
            }

            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ImportRow(lineIndex + 1, values));
        }

        return rows;
    }

    private static Dictionary<int, string> BuildHeaderMap(IReadOnlyDictionary<int, string> rawHeaders)
    {
        var result = new Dictionary<int, string>();
        foreach (var pair in rawHeaders)
        {
            var normalized = NormalizeHeader(pair.Value);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                result[pair.Key] = normalized;
            }
        }

        return result;
    }

    private static string NormalizeHeader(string? value)
    {
        var compact = NormalizeKey(value);

        return compact switch
        {
            "name" or "название" => "name",
            "subscription" or "подписка" => "subscription",
            "category" or "категория" => "category",
            "amount" or "sum" or "сумма" => "amount",
            "currency" or "валюта" => "currency",
            "cycle" or "billingcycle" or "period" or "период" => "cycle",
            "nextpayment" or "nextpaymentdate" or "nextcharge" or "следующеесписание" or "следующийплатеж" => "nextpayment",
            "firstpayment" or "firstpaymentdate" or "датапервогоплатежа" => "firstpayment",
            "description" or "описание" => "description",
            "status" or "статус" => "status",
            "isactive" or "активна" => "isactive",
            "autorenewal" or "автопродление" => "autorenewal",
            "reminderdays" or "reminderdaysbefore" or "напоминатьзадней" => "reminderdays",
            "islowusage" or "lowusage" or "редкоиспользую" => "islowusage",
            _ => compact
        };
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Trim()
            .ToUpperInvariant()
            .Where(static character => !char.IsWhiteSpace(character) && character is not '_' && character is not '-' && character is not '.')
            .ToArray());
    }

    private static char DetectSeparator(string headerLine)
    {
        var candidates = new[] { ';', ',', '\t' };
        return candidates
            .OrderByDescending(separator => headerLine.Count(character => character == separator))
            .First();
    }

    private static List<string> ParseCsvLine(string line, char separator)
    {
        var result = new List<string>();
        var buffer = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    buffer.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (!inQuotes && character == separator)
            {
                result.Add(buffer.ToString());
                buffer.Clear();
                continue;
            }

            buffer.Append(character);
        }

        result.Add(buffer.ToString());
        return result;
    }

    private sealed class ImportRow(int rowNumber, IReadOnlyDictionary<string, string> values)
    {
        public int RowNumber { get; } = rowNumber;

        public string GetRequired(params string[] keys)
        {
            var value = GetOptional(keys);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(LocalizationCatalog.Get("ImportMissingRequiredField"));
            }

            return value;
        }

        public string? GetOptional(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        public decimal GetRequiredDecimal(params string[] keys)
        {
            var value = GetRequired(keys);
            if (TryParseDecimal(value, out var amount))
            {
                return amount;
            }

            throw new InvalidOperationException(LocalizationCatalog.Format("ImportInvalidDecimal", value));
        }

        public int? GetOptionalInt(params string[] keys)
        {
            var value = GetOptional(keys);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed)
                    ? parsed
                    : throw new InvalidOperationException(LocalizationCatalog.Format("ImportInvalidInteger", value));
        }

        public DateOnly GetRequiredDate(params string[] keys)
        {
            var value = GetRequired(keys);
            if (TryParseDate(value, out var date))
            {
                return date;
            }

            throw new InvalidOperationException(LocalizationCatalog.Format("ImportInvalidDate", value));
        }

        public DateOnly? GetOptionalDate(params string[] keys)
        {
            var value = GetOptional(keys);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (TryParseDate(value, out var date))
            {
                return date;
            }

            throw new InvalidOperationException(LocalizationCatalog.Format("ImportInvalidDate", value));
        }

        public bool? GetOptionalBoolean(params string[] keys)
        {
            var value = GetOptional(keys);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = NormalizeKey(value);
            return normalized switch
            {
                "TRUE" or "YES" or "ДА" or "1" or "ACTIVE" or "АКТИВНА" => true,
                "FALSE" or "NO" or "НЕТ" or "0" or "DISABLED" or "ОТКЛЮЧЕНА" => false,
                _ => throw new InvalidOperationException(LocalizationCatalog.Format("ImportInvalidBoolean", value))
            };
        }

        public bool? GetOptionalStatus(params string[] keys)
        {
            var value = GetOptional(keys);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = NormalizeKey(value);
            return normalized switch
            {
                "ACTIVE" or "АКТИВНА" => true,
                "DISABLED" or "ОТКЛЮЧЕНА" => false,
                _ => null
            };
        }

        public BillingCycle GetRequiredBillingCycle(params string[] keys)
        {
            var value = GetRequired(keys);
            var normalized = NormalizeKey(value);

            return normalized switch
            {
                "1" or "MONTHLY" or "EVERYMONTH" or "КАЖДЫЙМЕСЯЦ" => BillingCycle.Monthly,
                "2" or "QUARTERLY" or "EVERYQUARTER" or "КАЖДЫЙКВАРТАЛ" => BillingCycle.Quarterly,
                "3" or "SEMIANNUAL" or "HALFYEAR" or "EVERYHALFYEAR" or "РАЗВПОЛГОДА" => BillingCycle.SemiAnnual,
                "4" or "YEARLY" or "ANNUAL" or "EVERYYEAR" or "РАЗВГОД" => BillingCycle.Yearly,
                _ => throw new InvalidOperationException(LocalizationCatalog.Format("ImportInvalidCycle", value))
            };
        }

        private static bool TryParseDecimal(string value, out decimal amount)
        {
            var sanitized = value.Replace(" ", string.Empty);
            return decimal.TryParse(sanitized, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
                || decimal.TryParse(sanitized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        private static bool TryParseDate(string value, out DateOnly date)
        {
            if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
            {
                return true;
            }

            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }

            return DateOnly.TryParseExact(
                value,
                ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }
    }
}
