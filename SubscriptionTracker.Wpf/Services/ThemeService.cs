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
            SetBrush(resources, "AppBackgroundBrush", "#F5F1EA");
            SetBrush(resources, "AppPanelBrush", "#FFF7ED");
            SetBrush(resources, "AppCardBrush", "#FFFFFF");
            SetBrush(resources, "AppSecondaryCardBrush", "#FCFAF7");
            SetBrush(resources, "AppAccentBrush", "#EA580C");
            SetBrush(resources, "AppAccentAltBrush", "#0284C7");
            SetBrush(resources, "AppForegroundBrush", "#0F172A");
            SetBrush(resources, "AppMutedBrush", "#57534E");
            SetBrush(resources, "AppBorderBrush", "#D6D3D1");
            SetBrush(resources, "AppHoverBrush", "#EEE7DA");
            SetBrush(resources, "AppPressedBrush", "#E7DDCC");
            SetBrush(resources, "AppInputBackgroundBrush", "#FFFFFF");
            SetBrush(resources, "AppTitleBarBrush", "#FCFAF7");
            SetBrush(resources, "AppDangerBrush", "#DC2626");
        }

        ThemeChanged?.Invoke(this, theme);
    }

    private static void SetBrush(ResourceDictionary resources, string key, string colorHex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
    }
}
