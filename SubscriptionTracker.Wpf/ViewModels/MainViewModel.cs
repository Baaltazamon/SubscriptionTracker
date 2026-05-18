using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeService _themeService;
    private object? _currentViewModel;
    private string _currentSectionTitle = "Панель управления";
    private NavigationSection _currentSection = NavigationSection.Dashboard;
    private DateTime _lastRefreshAt = DateTime.Now;

    public MainViewModel(
        DashboardViewModel dashboardViewModel,
        SubscriptionsViewModel subscriptionsViewModel,
        CalendarViewModel calendarViewModel,
        AnalyticsViewModel analyticsViewModel,
        PaymentHistoryViewModel paymentHistoryViewModel,
        SettingsViewModel settingsViewModel,
        AppEventBus eventBus,
        IAppSettingsService appSettingsService,
        IThemeService themeService)
    {
        _appSettingsService = appSettingsService;
        _themeService = themeService;
        Dashboard = dashboardViewModel;
        Subscriptions = subscriptionsViewModel;
        Calendar = calendarViewModel;
        Analytics = analyticsViewModel;
        PaymentHistory = paymentHistoryViewModel;
        Settings = settingsViewModel;

        NavigateDashboardCommand = new RelayCommand(() => Navigate(Dashboard, NavigationSection.Dashboard, "Панель управления"));
        NavigateSubscriptionsCommand = new RelayCommand(() => Navigate(Subscriptions, NavigationSection.Subscriptions, "Подписки"));
        NavigateCalendarCommand = new RelayCommand(() => Navigate(Calendar, NavigationSection.Calendar, "Календарь платежей"));
        NavigateAnalyticsCommand = new RelayCommand(() => Navigate(Analytics, NavigationSection.Analytics, "Аналитика"));
        NavigateHistoryCommand = new RelayCommand(() => Navigate(PaymentHistory, NavigationSection.History, "История платежей"));
        NavigateSettingsCommand = new RelayCommand(() => Navigate(Settings, NavigationSection.Settings, "Настройки"));

        eventBus.DataChanged += async (_, _) => await RefreshAllAsync();
        _themeService.ThemeChanged += (_, _) => RaiseBrandingProperties();

        Navigate(Dashboard, NavigationSection.Dashboard, "Панель управления");
    }

    public DashboardViewModel Dashboard { get; }

    public SubscriptionsViewModel Subscriptions { get; }

    public CalendarViewModel Calendar { get; }

    public AnalyticsViewModel Analytics { get; }

    public PaymentHistoryViewModel PaymentHistory { get; }

    public SettingsViewModel Settings { get; }

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentSectionTitle
    {
        get => _currentSectionTitle;
        private set => SetProperty(ref _currentSectionTitle, value);
    }

    public string CurrentSectionSubtitle => "Персональный контроль подписок";

    public string HeaderBadgeText => "Локально · SQLite · Без облака";

    public string SidebarStatusTitle => "Локальный режим";

    public string SidebarStatusDetails => "База: SQLite";

    public string LastRefreshLabel => FormatLastRefresh(_lastRefreshAt);

    public string WindowIconPath => GetPackUri("AppIconDark.png");

    public string TitleBarIconPath => _themeService.CurrentTheme == AppTheme.Dark
        ? GetPackUri("AppIconDark.png")
        : GetPackUri("AppIconLight.png");

    public string SidebarLogoPath => _themeService.CurrentTheme == AppTheme.Dark
        ? GetPackUri("LogoMenuDark.png")
        : GetPackUri("LogoMenuLight.png");

    public bool IsDashboardSelected => _currentSection == NavigationSection.Dashboard;

    public bool IsSubscriptionsSelected => _currentSection == NavigationSection.Subscriptions;

    public bool IsCalendarSelected => _currentSection == NavigationSection.Calendar;

    public bool IsAnalyticsSelected => _currentSection == NavigationSection.Analytics;

    public bool IsHistorySelected => _currentSection == NavigationSection.History;

    public bool IsSettingsSelected => _currentSection == NavigationSection.Settings;

    public RelayCommand NavigateDashboardCommand { get; }

    public RelayCommand NavigateSubscriptionsCommand { get; }

    public RelayCommand NavigateCalendarCommand { get; }

    public RelayCommand NavigateAnalyticsCommand { get; }

    public RelayCommand NavigateHistoryCommand { get; }

    public RelayCommand NavigateSettingsCommand { get; }

    public async Task InitializeAsync()
    {
        await RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        await Dashboard.RefreshAsync();
        await Subscriptions.RefreshAsync();
        await Calendar.RefreshAsync();
        await Analytics.RefreshAsync();
        await PaymentHistory.RefreshAsync();

        _lastRefreshAt = DateTime.Now;
        RaisePropertyChanged(nameof(LastRefreshLabel));
    }

    private void Navigate(object viewModel, NavigationSection section, string title)
    {
        CurrentViewModel = viewModel;
        CurrentSectionTitle = title;
        _currentSection = section;

        RaisePropertyChanged(nameof(IsDashboardSelected));
        RaisePropertyChanged(nameof(IsSubscriptionsSelected));
        RaisePropertyChanged(nameof(IsCalendarSelected));
        RaisePropertyChanged(nameof(IsAnalyticsSelected));
        RaisePropertyChanged(nameof(IsHistorySelected));
        RaisePropertyChanged(nameof(IsSettingsSelected));
    }

    private void RaiseBrandingProperties()
    {
        RaisePropertyChanged(nameof(TitleBarIconPath));
        RaisePropertyChanged(nameof(SidebarLogoPath));
    }

    private static string FormatLastRefresh(DateTime timestamp)
    {
        var now = DateTime.Now;
        return timestamp.Date == now.Date
            ? $"Последнее обновление: сегодня, {timestamp:HH:mm}"
            : $"Последнее обновление: {timestamp:dd.MM.yyyy HH:mm}";
    }

    private static string GetPackUri(string fileName)
    {
        return $"pack://application:,,,/Assets/Branding/{fileName}";
    }

    private enum NavigationSection
    {
        Dashboard = 1,
        Subscriptions = 2,
        Calendar = 3,
        Analytics = 4,
        History = 5,
        Settings = 6
    }
}
