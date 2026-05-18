using System.Windows;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class CategoryDeleteViewModel : ViewModelBase
{
    private OptionItem<Guid>? _selectedTransferOption;

    public CategoryDeleteViewModel(
        CategoryListItemDto currentItem,
        IReadOnlyList<CategoryListItemDto> availableCategories)
    {
        CategoryId = currentItem.Id;
        CategoryName = currentItem.Name;
        SubscriptionCount = currentItem.SubscriptionCount;

        TransferOptions = availableCategories
            .Where(item => item.Id != currentItem.Id)
            .OrderBy(item => item.Name)
            .Select(item => new OptionItem<Guid>
            {
                Value = item.Id,
                Label = item.Name
            })
            .ToArray();

        _selectedTransferOption = RequiresTransfer ? TransferOptions.FirstOrDefault() : null;
    }

    public Guid CategoryId { get; }

    public string CategoryName { get; }

    public int SubscriptionCount { get; }

    public bool RequiresTransfer => SubscriptionCount > 0;

    public Visibility TransferSectionVisibility => RequiresTransfer ? Visibility.Visible : Visibility.Collapsed;

    public string WindowTitle => LocalizationCatalog.Get("CategoryDeleteDialogTitle");

    public string Description => RequiresTransfer
        ? LocalizationCatalog.Format("CategoryDeleteTransferMessage", CategoryName, SubscriptionCount)
        : LocalizationCatalog.Format("CategoryDeleteConfirmMessage", CategoryName);

    public IReadOnlyList<OptionItem<Guid>> TransferOptions { get; }

    public OptionItem<Guid>? SelectedTransferOption
    {
        get => _selectedTransferOption;
        set => SetProperty(ref _selectedTransferOption, value);
    }

    public string? Validate()
    {
        if (RequiresTransfer && SelectedTransferOption is null)
        {
            return LocalizationCatalog.Get("CategoryTransferRequired");
        }

        return null;
    }

    public DeleteCategoryRequest BuildRequest()
    {
        return new DeleteCategoryRequest
        {
            CategoryId = CategoryId,
            TransferCategoryId = SelectedTransferOption?.Value
        };
    }
}
