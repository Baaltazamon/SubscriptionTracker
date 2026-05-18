using System.Windows;
using System.Windows.Media;

namespace SubscriptionTracker.Wpf.Services;

public sealed class ThemeService : IThemeService
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    public event EventHandler<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;

        var resources = System.Windows.Application.Current.Resources;
        if (theme == AppTheme.Dark)
        {
            SetBrush(resources, "AppBackgroundBrush", "#0F172A");
            SetBrush(resources, "AppPanelBrush", "#172554");
            SetBrush(resources, "AppCardBrush", "#111827");
            SetBrush(resources, "AppSecondaryCardBrush", "#1E293B");
            SetBrush(resources, "AppAccentBrush", "#F97316");
            SetBrush(resources, "AppAccentAltBrush", "#38BDF8");
            SetBrush(resources, "AppForegroundBrush", "#F8FAFC");
            SetBrush(resources, "AppMutedBrush", "#94A3B8");
            SetBrush(resources, "AppBorderBrush", "#334155");
            SetBrush(resources, "AppHoverBrush", "#243247");
            SetBrush(resources, "AppPressedBrush", "#334155");
            SetBrush(resources, "AppInputBackgroundBrush", "#0B1220");
            SetBrush(resources, "AppTitleBarBrush", "#0B1120");
            SetBrush(resources, "AppDangerBrush", "#DC2626");
        }
        else
        {
            SetBrush(resources, "AppBackgroundBrush", "#F3EDE3");
            SetBrush(resources, "AppPanelBrush", "#F7ECDE");
            SetBrush(resources, "AppCardBrush", "#FFFFFF");
            SetBrush(resources, "AppSecondaryCardBrush", "#F9F4EC");
            SetBrush(resources, "AppAccentBrush", "#EA580C");
            SetBrush(resources, "AppAccentAltBrush", "#0284C7");
            SetBrush(resources, "AppForegroundBrush", "#0F172A");
            SetBrush(resources, "AppMutedBrush", "#5F5548");
            SetBrush(resources, "AppBorderBrush", "#D2C5B2");
            SetBrush(resources, "AppHoverBrush", "#EFE3D2");
            SetBrush(resources, "AppPressedBrush", "#E6D7C1");
            SetBrush(resources, "AppInputBackgroundBrush", "#FFFCF8");
            SetBrush(resources, "AppTitleBarBrush", "#FAF4EB");
            SetBrush(resources, "AppDangerBrush", "#DC2626");
        }

        ThemeChanged?.Invoke(this, theme);
    }

    private static void SetBrush(ResourceDictionary resources, string key, string colorHex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
    }
}
