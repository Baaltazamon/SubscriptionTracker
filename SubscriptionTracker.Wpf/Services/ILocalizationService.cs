namespace SubscriptionTracker.Wpf.Services;

public interface ILocalizationService
{
    event EventHandler<string>? LanguageChanged;

    string CurrentLanguageCode { get; }

    void ApplyLanguage(string languageCode);
}
