using System.Windows;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Wpf.Dialogs;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf.Services;

public sealed class CategoryManagementDialogService(IDialogService dialogService) : ICategoryManagementDialogService
{
    public Task<SaveCategoryRequest?> ShowEditorAsync(CategoryListItemDto? currentItem, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new CategoryEditViewModel(currentItem);
        var window = new CategoryEditWindow(dialogService)
        {
            DataContext = viewModel
        };

        SetOwner(window);
        return Task.FromResult(window.ShowDialog() == true ? viewModel.BuildRequest() : null);
    }

    public Task<DeleteCategoryRequest?> ShowDeleteAsync(
        CategoryListItemDto currentItem,
        IReadOnlyList<CategoryListItemDto> availableCategories,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new CategoryDeleteViewModel(currentItem, availableCategories);
        var window = new CategoryDeleteWindow(dialogService)
        {
            DataContext = viewModel
        };

        SetOwner(window);
        return Task.FromResult(window.ShowDialog() == true ? viewModel.BuildRequest() : null);
    }

    private static void SetOwner(Window window)
    {
        var owner = System.Windows.Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(item => item.IsActive && !ReferenceEquals(item, window));

        if (owner is null && !ReferenceEquals(System.Windows.Application.Current.MainWindow, window))
        {
            owner = System.Windows.Application.Current.MainWindow;
        }

        if (owner is not null && !ReferenceEquals(owner, window))
        {
            window.Owner = owner;
        }
    }
}
