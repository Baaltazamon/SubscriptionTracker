using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Infrastructure.Persistence;
using System.Text.RegularExpressions;

namespace SubscriptionTracker.Infrastructure;

public sealed class CategoryService(AppDbContext dbContext) : ICategoryService
{
    private static readonly Regex ColorHexPattern = new("^#([0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<CategoryOptionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(static category => category.Name)
            .Select(static category => new CategoryOptionDto
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryListItemDto>> GetManageableAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(static category => category.IsSystem ? 0 : 1)
            .ThenBy(static category => category.Name)
            .Select(static category => new CategoryListItemDto
            {
                Id = category.Id,
                Name = category.Name,
                ColorHex = category.ColorHex,
                IsSystem = category.IsSystem,
                SubscriptionCount = category.Subscriptions.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryListItemDto> SaveAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("CategoryNameRequired"));
        }

        var colorHex = NormalizeColorHex(request.ColorHex);
        if (colorHex is not null && !ColorHexPattern.IsMatch(colorHex))
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("CategoryColorInvalid"));
        }

        var normalizedName = name.ToUpperInvariant();
        var duplicateExists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(category =>
                category.Id != request.Id &&
                category.Name.ToUpper() == normalizedName,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("CategoryDuplicateName"));
        }

        Domain.Entities.Category category;
        if (request.Id is { } categoryId)
        {
            category = await dbContext.Categories
                .FirstOrDefaultAsync(item => item.Id == categoryId, cancellationToken)
                ?? throw new InvalidOperationException(LocalizationCatalog.Get("CategoryNotFound"));

            category.Name = name;
            category.ColorHex = colorHex;
        }
        else
        {
            category = new Domain.Entities.Category
            {
                Id = Guid.NewGuid(),
                Name = name,
                ColorHex = colorHex,
                IsSystem = false
            };

            await dbContext.Categories.AddAsync(category, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var subscriptionCount = await dbContext.Subscriptions
            .AsNoTracking()
            .CountAsync(subscription => subscription.CategoryId == category.Id, cancellationToken);

        return new CategoryListItemDto
        {
            Id = category.Id,
            Name = category.Name,
            ColorHex = category.ColorHex,
            IsSystem = category.IsSystem,
            SubscriptionCount = subscriptionCount
        };
    }

    public async Task DeleteAsync(DeleteCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .Include(item => item.Subscriptions)
            .FirstOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new InvalidOperationException(LocalizationCatalog.Get("CategoryNotFound"));

        if (category.IsSystem)
        {
            throw new InvalidOperationException(LocalizationCatalog.Get("CategoryDeleteSystemError"));
        }

        if (category.Subscriptions.Count > 0)
        {
            if (request.TransferCategoryId is null || request.TransferCategoryId == category.Id)
            {
                throw new InvalidOperationException(LocalizationCatalog.Get("CategoryTransferRequired"));
            }

            var transferCategory = await dbContext.Categories
                .FirstOrDefaultAsync(item => item.Id == request.TransferCategoryId.Value, cancellationToken)
                ?? throw new InvalidOperationException(LocalizationCatalog.Get("CategoryTransferNotFound"));

            foreach (var subscription in category.Subscriptions)
            {
                subscription.CategoryId = transferCategory.Id;
                subscription.Category = transferCategory;
            }
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeColorHex(string? colorHex)
    {
        return string.IsNullOrWhiteSpace(colorHex)
            ? null
            : colorHex.Trim().ToUpperInvariant();
    }
}
