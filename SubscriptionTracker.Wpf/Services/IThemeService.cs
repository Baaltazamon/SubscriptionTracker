namespace SubscriptionTracker.Wpf.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    event EventHandler<AppTheme>? ThemeChanged;

    void Apply(AppTheme theme);
}
