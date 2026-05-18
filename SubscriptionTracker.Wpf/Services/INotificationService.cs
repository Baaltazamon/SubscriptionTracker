namespace SubscriptionTracker.Wpf.Services;

public interface INotificationService
{
    void ShowInfo(string message, string title);

    void ShowError(string message, string title);

    bool Confirm(string message, string title);
}
