using System.Windows;
using SubscriptionTracker.Wpf.Dialogs;

namespace SubscriptionTracker.Wpf.Services;

public sealed class DialogService : IDialogService
{
    public DialogResultKind Show(DialogRequest request)
    {
        var window = new DialogWindow(request);
        var owner = GetOwnerWindow();
        if (owner is not null)
        {
            window.Owner = owner;
        }

        window.ShowDialog();
        return window.Result;
    }

    public void ShowInfo(string message, string title)
    {
        Show(new DialogRequest
        {
            Title = title,
            Message = message,
            Kind = DialogKind.Info
        });
    }

    public void ShowWarning(string message, string title)
    {
        Show(new DialogRequest
        {
            Title = title,
            Message = message,
            Kind = DialogKind.Warning
        });
    }

    public void ShowError(string message, string title)
    {
        Show(new DialogRequest
        {
            Title = title,
            Message = message,
            Kind = DialogKind.Error
        });
    }

    public bool Confirm(
        string message,
        string title,
        DialogKind kind = DialogKind.Confirm,
        DialogButton primaryButton = DialogButton.Ok,
        DialogButton secondaryButton = DialogButton.Cancel)
    {
        var result = Show(new DialogRequest
        {
            Title = title,
            Message = message,
            Kind = kind,
            PrimaryButton = primaryButton,
            SecondaryButton = secondaryButton
        });

        return result == MapResult(primaryButton);
    }

    private static Window? GetOwnerWindow()
    {
        return System.Windows.Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(static window => window.IsActive)
            ?? System.Windows.Application.Current.MainWindow;
    }

    private static DialogResultKind MapResult(DialogButton button)
    {
        return button switch
        {
            DialogButton.Ok => DialogResultKind.Ok,
            DialogButton.Cancel => DialogResultKind.Cancel,
            DialogButton.Delete => DialogResultKind.Delete,
            DialogButton.Restore => DialogResultKind.Restore,
            _ => DialogResultKind.None
        };
    }
}
