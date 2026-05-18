namespace SubscriptionTracker.Wpf.Dialogs;

public sealed class DialogRequest
{
    public required string Title { get; init; }

    public required string Message { get; init; }

    public DialogKind Kind { get; init; } = DialogKind.Info;

    public DialogButton PrimaryButton { get; init; } = DialogButton.Ok;

    public DialogButton? SecondaryButton { get; init; }
}
