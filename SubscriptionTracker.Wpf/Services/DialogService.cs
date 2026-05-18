using System.Windows;
using SubscriptionTracker.Wpf.Dialogs;

namespace SubscriptionTracker.Wpf.Services;

public sealed class DialogService : IDialogService
{
    public DialogResultKind Show(DialogRequest request)
    {
        var window = new DialogWindow(request);
        var owner = GetOwnerWindow(window);
        if (owner is not null && !ReferenceEquals(owner, window))
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

    private static Window? GetOwnerWindow(Window dialogWindow)
    {
        var owner = System.Windows.Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive && !ReferenceEquals(window, dialogWindow));

        if (owner is null && !ReferenceEquals(System.Windows.Application.Current.MainWindow, dialogWindow))
        {
            owner = System.Windows.Application.Current.MainWindow;
        }

        return owner;
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
