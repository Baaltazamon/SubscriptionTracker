using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Wpf.Services;

public interface ICategoryManagementDialogService
{
    Task<SaveCategoryRequest?> ShowEditorAsync(CategoryListItemDto? currentItem, CancellationToken cancellationToken = default);

    Task<DeleteCategoryRequest?> ShowDeleteAsync(
        CategoryListItemDto currentItem,
        IReadOnlyList<CategoryListItemDto> availableCategories,
        CancellationToken cancellationToken = default);
}
