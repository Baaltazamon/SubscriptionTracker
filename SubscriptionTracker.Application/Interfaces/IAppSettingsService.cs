using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface IAppSettingsService
{
    event EventHandler<AppSettingsDto>? SettingsChanged;

    AppSettingsDto GetSettings();

    Task SaveAsync(AppSettingsDto settings, CancellationToken cancellationToken = default);
}
