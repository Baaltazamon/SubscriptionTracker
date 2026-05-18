using System.Globalization;
using System.Windows;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string LocalizationDictionaryPrefix = "Resources/Localization/Strings.";

    public string CurrentLanguageCode { get; private set; } = "ru-RU";

    public event EventHandler<string>? LanguageChanged;

    public void ApplyLanguage(string languageCode)
    {
        var normalized = LocalizationCatalog.NormalizeLanguageCode(languageCode);
        CurrentLanguageCode = normalized;

        var culture = new CultureInfo(normalized);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        LocalizationCatalog.SetLanguage(normalized);
        ReplaceLocalizationDictionary(normalized);

        LanguageChanged?.Invoke(this, normalized);
    }

    private static void ReplaceLocalizationDictionary(string languageCode)
    {
        var resources = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existing = resources.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains(LocalizationDictionaryPrefix, StringComparison.OrdinalIgnoreCase) == true);

        if (existing is not null)
        {
            resources.Remove(existing);
        }

        resources.Insert(0, new ResourceDictionary
        {
            Source = new Uri($"{LocalizationDictionaryPrefix}{languageCode}.xaml", UriKind.Relative)
        });
    }
}
