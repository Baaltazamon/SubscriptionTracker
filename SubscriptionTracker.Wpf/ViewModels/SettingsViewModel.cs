using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeService _themeService;

    public SettingsViewModel(IAppSettingsService appSettingsService, IThemeService themeService)
    {
        _appSettingsService = appSettingsService;
        _themeService = themeService;

        UseDarkThemeCommand = new RelayCommand(() =>
        {
            _themeService.Apply(AppTheme.Dark);
            RaisePropertyChanged(nameof(ThemeName));
        });

        UseLightThemeCommand = new RelayCommand(() =>
        {
            _themeService.Apply(AppTheme.Light);
            RaisePropertyChanged(nameof(ThemeName));
        });
    }

    public string BaseCurrency => _appSettingsService.GetSettings().BaseCurrency;

    public string DatabasePath => _appSettingsService.GetSettings().DatabasePath;

    public int ReminderCheckIntervalMinutes => _appSettingsService.GetSettings().ReminderCheckIntervalMinutes;

    public bool NotificationsEnabled => _appSettingsService.GetSettings().NotificationsEnabled;

    public string ThemeName => _themeService.CurrentTheme == AppTheme.Dark ? "Темная" : "Светлая";

    public string StorageMode => "SQLite, локально на устройстве";

    public string BackupStatus => "Резервное копирование пока не настроено";

    public string AppVersion => ".NET 8 · MVP build";

    public RelayCommand UseDarkThemeCommand { get; }

    public RelayCommand UseLightThemeCommand { get; }
}
