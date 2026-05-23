using System.Windows;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Wpf.Dialogs;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf.Services;

public sealed class ImportPreviewDialogService : IImportPreviewDialogService
{
    public Task<IReadOnlyList<int>?> ShowAsync(ImportSubscriptionsPreviewDto preview, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new ImportPreviewViewModel(preview);
        var window = new ImportPreviewWindow
        {
            DataContext = viewModel
        };

        SetOwner(window);
        return Task.FromResult<IReadOnlyList<int>?>(window.ShowDialog() == true ? viewModel.SelectedRowNumbers : null);
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
