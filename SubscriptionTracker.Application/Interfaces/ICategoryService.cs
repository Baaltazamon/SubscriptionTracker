using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryOptionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryListItemDto>> GetManageableAsync(CancellationToken cancellationToken = default);

    Task<CategoryListItemDto> SaveAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteCategoryRequest request, CancellationToken cancellationToken = default);
}
