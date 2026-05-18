using SubscriptionTracker.Application.DTO;

namespace SubscriptionTracker.Application.Interfaces;

public interface IAppSettingsService
{
    AppSettingsDto GetSettings();
}
