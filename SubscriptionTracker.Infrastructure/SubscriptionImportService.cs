using System.Globalization;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ImportSubscriptionsPreviewDto> PreviewAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var plan = await BuildImportPlanAsync(filePath, cancellationToken);

        return new ImportSubscriptionsPreviewDto
        {
            TotalRows = plan.TotalRows,
            CreatedCount = plan.CreatedCount,
            UpdatedCount = plan.UpdatedCount,
            CreatedCategoryCount = plan.CreatedCategoryCount,
            SkippedCount = plan.SkippedCount,
            Warnings = plan.Warnings,
            Items = plan.Items
                .Select(static item => new ImportPreviewItemDto
                {
                    RowNumber = item.RowNumber,
                    Name = item.Name,
                    CategoryName = item.CategoryName,
                    Amount = item.Amount,
                    Currency = item.Currency,
                    BillingCycle = item.BillingCycle,
                    NextPaymentDate = item.NextPaymentDate,
                    Action = item.Action,
                    WillCreateCategory = item.WillCreateCategory,
                    Note = item.Note
                })
                .ToArray()
        };
    }

    public async Task<ImportSubscriptionsResultDto> ImportAsync(
        string filePath,
        IReadOnlyCollection<int>? selectedRowNumbers = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await BuildImportPlanAsync(filePath, cancellationToken);
        var selectedRows = selectedRowNumbers?.ToHashSet() ?? [];
        var actionableItems = plan.Items
            .Where(static item => item.Action is ImportPreviewAction.Create or ImportPreviewAction.Update)
            .ToArray();
        var itemsToImport = selectedRowNumbers is null
            ? actionableItems
            : actionableItems.Where(item => selectedRows.Contains(item.RowNumber)).ToArray();

        if (itemsToImport.Length == 0)
        {
            return new ImportSubscriptionsResultDto
            {
                TotalRows = plan.TotalRows,
                CreatedCount = 0,
                UpdatedCount = 0,
                CreatedCategoryCount = 0,
                SkippedCount = plan.SkippedCount,
                IgnoredCount = actionableItems.Length,
                Warnings = plan.Warnings
            };
        }

        var categoriesByName = await dbContext.Categories
            .ToDictionaryAsync(static item => NormalizeKey(item.Name), cancellationToken);

        var createdCategories = new HashSet<string>(StringComparer.Ordinal);
        var importSession = new ImportSession
        {
            Id = Guid.NewGuid(),
            SourceFileName = Path.GetFileName(filePath),
            CreatedAtUtc = DateTime.UtcNow,
            AppliedRowsCount = itemsToImport.Length,
            CreatedCount = itemsToImport.Count(static item => item.Action == ImportPreviewAction.Create),
            UpdatedCount = itemsToImport.Count(static item => item.Action == ImportPreviewAction.Update),
            CreatedCategoryCount = 0
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.ImportSessions.AddAsync(importSession, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in itemsToImport)
        {
            SubscriptionSnapshot? snapshot = null;
            if (item.Action == ImportPreviewAction.Update && item.SubscriptionId is { } subscriptionId)
            {
                snapshot = await CreateSnapshotAsync(subscriptionId, cancellationToken);
            }

            var categoryKey = NormalizeKey(item.CategoryName);
            if (!categoriesByName.TryGetValue(categoryKey, out var category))
            {
                category = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = item.CategoryName.Trim(),
                    ColorHex = DefaultCategoryColor,
                    IsSystem = false
                };

                await dbContext.Categories.AddAsync(category, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                categoriesByName[categoryKey] = category;
                createdCategories.Add(categoryKey);
                importSession.CreatedCategoryCount = createdCategories.Count;

                await dbContext.ImportSessionEntries.AddAsync(new ImportSessionEntry
                {
                    Id = Guid.NewGuid(),
                    ImportSessionId = importSession.Id,
                    Kind = ImportSessionEntryKind.CategoryCreated,
                    EntityId = category.Id,
                    DisplayName = category.Name
                }, cancellationToken);
            }

            var request = new SaveSubscriptionRequest
            {
                Id = item.SubscriptionId,
                Name = item.Name,
                Description = item.Description,
                CategoryId = category.Id,
                Amount = item.Amount,
                Currency = item.Currency,
                BillingCycle = item.BillingCycle,
                FirstPaymentDate = item.FirstPaymentDate,
                NextPaymentDate = item.NextPaymentDate,
                IsActive = item.IsActive,
                AutoRenewal = item.AutoRenewal,
                ReminderDaysBefore = item.ReminderDaysBefore,
                IsLowUsage = item.IsLowUsage
            };

            var saved = await subscriptionService.SaveAsync(request, cancellationToken);

            await dbContext.ImportSessionEntries.AddAsync(new ImportSessionEntry
            {
                Id = Guid.NewGuid(),
                ImportSessionId = importSession.Id,
                Kind = item.Action == ImportPreviewAction.Create
                    ? ImportSessionEntryKind.SubscriptionCreated
                    : ImportSessionEntryKind.SubscriptionUpdated,
                EntityId = saved.Id,
                DisplayName = saved.Name,
                SnapshotJson = snapshot is null ? null : JsonSerializer.Serialize(snapshot, SnapshotJsonOptions)
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ImportSubscriptionsResultDto
        {
            TotalRows = plan.TotalRows,
            CreatedCount = itemsToImport.Count(static item => item.Action == ImportPreviewAction.Create),
            UpdatedCount = itemsToImport.Count(static item => item.Action == ImportPreviewAction.Update),
            CreatedCategoryCount = createdCategories.Count,
            SkippedCount = plan.SkippedCount,
            IgnoredCount = actionableItems.Length - itemsToImport.Length,
            Warnings = plan.Warnings
        };
    }

    private async Task<SubscriptionSnapshot> CreateSnapshotAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(static item => item.Payments)
            .FirstAsync(item => item.Id == subscriptionId, cancellationToken);

        return new SubscriptionSnapshot
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Description = subscription.Description,
            CategoryId = subscription.CategoryId,
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            BillingCycle = subscription.BillingCycle,
            FirstPaymentDate = subscription.FirstPaymentDate,
            NextPaymentDate = subscription.NextPaymentDate,
            IsActive = subscription.IsActive,
            AutoRenewal = subscription.AutoRenewal,
            ReminderDaysBefore = subscription.ReminderDaysBefore,
            IsLowUsage = subscription.IsLowUsage,
            LastUsedDate = subscription.LastUsedDate,
            CreatedAtUtc = subscription.CreatedAtUtc,
            UpdatedAtUtc = subscription.UpdatedAtUtc,
            Payments = subscription.Payments
                .Select(static payment => new PaymentHistorySnapshot
                {
                    Id = payment.Id,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    PaymentDate = payment.PaymentDate,
                    Status = payment.Status,
                    Note = payment.Note,
                    CreatedAtUtc = payment.CreatedAtUtc
                })
                .ToList()
        };
    }

    private async Task<ImportPlan> BuildImportPlanAsync(string filePath, CancellationToken cancellationToken)
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
            .AsNoTracking()
            .ToDictionaryAsync(static item => NormalizeKey(item.Name), cancellationToken);

        var subscriptionsByName = await dbContext.Subscriptions
            .AsNoTracking()
            .ToDictionaryAsync(static item => NormalizeKey(item.Name), cancellationToken);

        var reservedNewCategories = new HashSet<string>(StringComparer.Ordinal);
        var warnings = new List<string>();
        var items = new List<ImportPlanItem>();
        var createdCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var name = row.GetRequired("name", "subscription").Trim();
                var key = NormalizeKey(name);
                subscriptionsByName.TryGetValue(key, out var existing);

                var categoryName = row.GetRequired("category").Trim();
                var categoryKey = NormalizeKey(categoryName);
                var willCreateCategory = !categoriesByName.ContainsKey(categoryKey) && reservedNewCategories.Add(categoryKey);

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

                var amount = row.GetRequiredDecimal("amount", "sum");
                var currency = row.GetOptional("currency")?.Trim().ToUpperInvariant() ?? "RUB";
                var billingCycle = row.GetRequiredBillingCycle("cycle", "billingcycle", "period");
                var isActive = row.GetOptionalBoolean("isactive")
                    ?? row.GetOptionalStatus("status")
                    ?? true;
                var autoRenewal = row.GetOptionalBoolean("autorenewal", "auto") ?? true;
                var reminderDays = row.GetOptionalInt("reminderdays", "reminderdaysbefore", "reminder") ?? 3;
                var isLowUsage = row.GetOptionalBoolean("islowusage", "lowusage") ?? false;

                var action = existing is null ? ImportPreviewAction.Create : ImportPreviewAction.Update;
                if (action == ImportPreviewAction.Create)
                {
                    createdCount++;
                }
                else
                {
                    updatedCount++;
                }

                items.Add(new ImportPlanItem
                {
                    RowNumber = row.RowNumber,
                    SubscriptionId = existing?.Id,
                    Name = name,
                    CategoryName = categoryName,
                    Amount = amount,
                    Currency = currency,
                    BillingCycle = billingCycle,
                    FirstPaymentDate = firstPaymentDate,
                    NextPaymentDate = nextPaymentDate,
                    Description = row.GetOptional("description"),
                    IsActive = isActive,
                    AutoRenewal = autoRenewal,
                    ReminderDaysBefore = reminderDays,
                    IsLowUsage = isLowUsage,
                    Action = action,
                    WillCreateCategory = willCreateCategory,
                    Note = willCreateCategory ? LocalizationCatalog.Get("ImportPreviewNewCategoryNote") : null
                });
            }
            catch (Exception exception)
            {
                skippedCount++;
                var warning = LocalizationCatalog.Format("ImportWarningRowFormat", row.RowNumber, exception.Message);
                warnings.Add(warning);
                items.Add(new ImportPlanItem
                {
                    RowNumber = row.RowNumber,
                    Action = ImportPreviewAction.Skip,
                    Note = exception.Message
                });
            }
        }

        return new ImportPlan
        {
            TotalRows = rows.Count,
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            CreatedCategoryCount = reservedNewCategories.Count,
            SkippedCount = skippedCount,
            Warnings = warnings,
            Items = items
        };
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
            "name" or "РЅР°Р·РІР°РЅРёРµ" => "name",
            "subscription" or "РїРѕРґРїРёСЃРєР°" => "subscription",
            "category" or "РєР°С‚РµРіРѕСЂРёСЏ" => "category",
            "amount" or "sum" or "СЃСѓРјРјР°" => "amount",
            "currency" or "РІР°Р»СЋС‚Р°" => "currency",
            "cycle" or "billingcycle" or "period" or "РїРµСЂРёРѕРґ" => "cycle",
            "nextpayment" or "nextpaymentdate" or "nextcharge" or "СЃР»РµРґСѓСЋС‰РµРµСЃРїРёСЃР°РЅРёРµ" or "СЃР»РµРґСѓСЋС‰РёР№РїР»Р°С‚РµР¶" => "nextpayment",
            "firstpayment" or "firstpaymentdate" or "РґР°С‚Р°РїРµСЂРІРѕРіРѕРїР»Р°С‚РµР¶Р°" => "firstpayment",
            "description" or "РѕРїРёСЃР°РЅРёРµ" => "description",
            "status" or "СЃС‚Р°С‚СѓСЃ" => "status",
            "isactive" or "Р°РєС‚РёРІРЅР°" => "isactive",
            "autorenewal" or "Р°РІС‚РѕРїСЂРѕРґР»РµРЅРёРµ" => "autorenewal",
            "reminderdays" or "reminderdaysbefore" or "РЅР°РїРѕРјРёРЅР°С‚СЊР·Р°РґРЅРµР№" => "reminderdays",
            "islowusage" or "lowusage" or "СЂРµРґРєРѕРёСЃРїРѕР»СЊР·СѓСЋ" => "islowusage",
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

    private sealed class ImportPlan
    {
        public int TotalRows { get; init; }

        public int CreatedCount { get; init; }

        public int UpdatedCount { get; init; }

        public int CreatedCategoryCount { get; init; }

        public int SkippedCount { get; init; }

        public IReadOnlyList<ImportPlanItem> Items { get; init; } = [];

        public IReadOnlyList<string> Warnings { get; init; } = [];
    }

    private sealed class ImportPlanItem
    {
        public int RowNumber { get; init; }

        public Guid? SubscriptionId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string CategoryName { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public string Currency { get; init; } = "RUB";

        public BillingCycle BillingCycle { get; init; }

        public DateOnly FirstPaymentDate { get; init; }

        public DateOnly NextPaymentDate { get; init; }

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public bool AutoRenewal { get; init; }

        public int ReminderDaysBefore { get; init; }

        public bool IsLowUsage { get; init; }

        public ImportPreviewAction Action { get; init; }

        public bool WillCreateCategory { get; init; }

        public string? Note { get; init; }
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
                "TRUE" or "YES" or "Р”Рђ" or "1" or "ACTIVE" or "РђРљРўРР’РќРђ" => true,
                "FALSE" or "NO" or "РќР•Рў" or "0" or "DISABLED" or "РћРўРљР›Р®Р§Р•РќРђ" => false,
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
                "ACTIVE" or "РђРљРўРР’РќРђ" => true,
                "DISABLED" or "РћРўРљР›Р®Р§Р•РќРђ" => false,
                _ => null
            };
        }

        public BillingCycle GetRequiredBillingCycle(params string[] keys)
        {
            var value = GetRequired(keys);
            var normalized = NormalizeKey(value);

            return normalized switch
            {
                "1" or "MONTHLY" or "EVERYMONTH" or "РљРђР–Р”Р«Р™РњР•РЎРЇР¦" => BillingCycle.Monthly,
                "2" or "QUARTERLY" or "EVERYQUARTER" or "РљРђР–Р”Р«Р™РљР’РђР РўРђР›" => BillingCycle.Quarterly,
                "3" or "SEMIANNUAL" or "HALFYEAR" or "EVERYHALFYEAR" or "Р РђР—Р’РџРћР›Р“РћР”Рђ" => BillingCycle.SemiAnnual,
                "4" or "YEARLY" or "ANNUAL" or "EVERYYEAR" or "Р РђР—Р’Р“РћР”" => BillingCycle.Yearly,
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

    internal sealed class SubscriptionSnapshot
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public Guid CategoryId { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = "RUB";

        public BillingCycle BillingCycle { get; init; }

        public DateOnly FirstPaymentDate { get; init; }

        public DateOnly NextPaymentDate { get; init; }

        public bool IsActive { get; init; }

        public bool AutoRenewal { get; init; }

        public int ReminderDaysBefore { get; init; }

        public bool IsLowUsage { get; init; }

        public DateOnly? LastUsedDate { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? UpdatedAtUtc { get; init; }

        public List<PaymentHistorySnapshot> Payments { get; init; } = [];
    }

    internal sealed class PaymentHistorySnapshot
    {
        public Guid Id { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = "RUB";

        public DateOnly PaymentDate { get; init; }

        public PaymentStatus Status { get; init; }

        public string? Note { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }
}
