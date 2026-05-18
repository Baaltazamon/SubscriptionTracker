namespace SubscriptionTracker.Wpf.Services;

public interface IAutoStartService
{
    bool IsEnabled();

    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}
