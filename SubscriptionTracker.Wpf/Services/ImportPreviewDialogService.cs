using System.Windows;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Wpf.Dialogs;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf.Services;

public sealed class ImportPreviewDialogService : IImportPreviewDialogService
{
    public Task<bool> ShowAsync(ImportSubscriptionsPreviewDto preview, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = new ImportPreviewWindow
        {
            DataContext = new ImportPreviewViewModel(preview)
        };

        SetOwner(window);
        return Task.FromResult(window.ShowDialog() == true);
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
