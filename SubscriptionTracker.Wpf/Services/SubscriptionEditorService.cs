using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf.Services;

public sealed class SubscriptionEditorService(
    IServiceScopeFactory scopeFactory,
    IDialogService dialogService) : ISubscriptionEditorService
{
    public async Task<bool> ShowAsync(SubscriptionListItemDto? currentItem, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        var viewModel = await SubscriptionEditViewModel.CreateAsync(categoryService, currentItem, cancellationToken);
        var window = new SubscriptionEditWindow(dialogService)
        {
            DataContext = viewModel
        };

        if (window.ShowDialog() != true)
        {
            return false;
        }

        try
        {
            await subscriptionService.SaveAsync(viewModel.BuildRequest(), cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            dialogService.ShowError(exception.Message, LocalizationCatalog.Get("SaveFailedTitle"));
            return false;
        }
    }
}
