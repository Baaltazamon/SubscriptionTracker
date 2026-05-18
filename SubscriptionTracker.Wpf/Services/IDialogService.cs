using SubscriptionTracker.Wpf.Dialogs;

namespace SubscriptionTracker.Wpf.Services;

public interface IDialogService
{
    DialogResultKind Show(DialogRequest request);

    void ShowInfo(string message, string title);

    void ShowWarning(string message, string title);

    void ShowError(string message, string title);

    bool Confirm(
        string message,
        string title,
        DialogKind kind = DialogKind.Confirm,
        DialogButton primaryButton = DialogButton.Ok,
        DialogButton secondaryButton = DialogButton.Cancel);
}
