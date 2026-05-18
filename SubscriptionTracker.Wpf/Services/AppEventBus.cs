namespace SubscriptionTracker.Wpf.Services;

public sealed class AppEventBus
{
    public event EventHandler? DataChanged;

    public void PublishDataChanged()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
