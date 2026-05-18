namespace SubscriptionTracker.Wpf.Services;

public sealed class AppEventBus
{
    public event EventHandler? DataChanged;
    public event EventHandler? SettingsChanged;

    public void PublishDataChanged()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void PublishSettingsChanged()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
