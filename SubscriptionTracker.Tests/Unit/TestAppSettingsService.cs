using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;

namespace SubscriptionTracker.Tests.Unit;

internal sealed class TestAppSettingsService(AppSettingsDto settings) : IAppSettingsService
{
    private AppSettingsDto _settings = settings;

    public event EventHandler<AppSettingsDto>? SettingsChanged;

    public AppSettingsDto GetSettings() => _settings;

    public Task SaveAsync(AppSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        SettingsChanged?.Invoke(this, settings);
        return Task.CompletedTask;
    }
}
