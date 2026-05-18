using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private object? _currentViewModel;
    private string _currentSectionTitle = string.Empty;
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
        IThemeService themeService,
        ILocalizationService localizationService)
    {
        _themeService = themeService;
        _localizationService = localizationService;
        Dashboard = dashboardViewModel;
        Subscriptions = subscriptionsViewModel;
        Calendar = calendarViewModel;
        Analytics = analyticsViewModel;
        PaymentHistory = paymentHistoryViewModel;
        Settings = settingsViewModel;

        NavigateDashboardCommand = new RelayCommand(() => Navigate(Dashboard, NavigationSection.Dashboard));
        NavigateSubscriptionsCommand = new RelayCommand(() => Navigate(Subscriptions, NavigationSection.Subscriptions));
        NavigateCalendarCommand = new RelayCommand(() => Navigate(Calendar, NavigationSection.Calendar));
        NavigateAnalyticsCommand = new RelayCommand(() => Navigate(Analytics, NavigationSection.Analytics));
        NavigateHistoryCommand = new RelayCommand(() => Navigate(PaymentHistory, NavigationSection.History));
        NavigateSettingsCommand = new RelayCommand(() => Navigate(Settings, NavigationSection.Settings));

        eventBus.DataChanged += async (_, _) => await RefreshAllAsync();
        eventBus.SettingsChanged += async (_, _) => await RefreshAllAsync();
        _themeService.ThemeChanged += (_, _) => RaiseBrandingProperties();
        _localizationService.LanguageChanged += (_, _) => RaiseLocalizedProperties();

        Navigate(Dashboard, NavigationSection.Dashboard);
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

    public string CurrentSectionSubtitle => LocalizationCatalog.Get("MainSubtitle");

    public string HeaderBadgeText => LocalizationCatalog.Get("HeaderBadgeText");

    public string SidebarStatusTitle => LocalizationCatalog.Get("SidebarStatusTitle");

    public string SidebarStatusDetails => LocalizationCatalog.Get("SidebarStatusDetails");

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

    private void Navigate(object viewModel, NavigationSection section)
    {
        CurrentViewModel = viewModel;
        _currentSection = section;
        CurrentSectionTitle = GetSectionTitle(section);

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

    private void RaiseLocalizedProperties()
    {
        CurrentSectionTitle = GetSectionTitle(_currentSection);
        RaisePropertyChanged(nameof(CurrentSectionSubtitle));
        RaisePropertyChanged(nameof(HeaderBadgeText));
        RaisePropertyChanged(nameof(SidebarStatusTitle));
        RaisePropertyChanged(nameof(SidebarStatusDetails));
        RaisePropertyChanged(nameof(LastRefreshLabel));
    }

    private static string FormatLastRefresh(DateTime timestamp)
    {
        var now = DateTime.Now;
        return timestamp.Date == now.Date
            ? LocalizationCatalog.Format("LastRefreshTodayFormat", timestamp)
            : LocalizationCatalog.Format("LastRefreshDateFormat", timestamp);
    }

    private static string GetPackUri(string fileName)
    {
        return $"pack://application:,,,/Assets/Branding/{fileName}";
    }

    private static string GetSectionTitle(NavigationSection section)
    {
        return section switch
        {
            NavigationSection.Dashboard => LocalizationCatalog.Get("DashboardNav"),
            NavigationSection.Subscriptions => LocalizationCatalog.Get("SubscriptionsNav"),
            NavigationSection.Calendar => LocalizationCatalog.Get("CalendarNav"),
            NavigationSection.Analytics => LocalizationCatalog.Get("AnalyticsNav"),
            NavigationSection.History => LocalizationCatalog.Get("HistoryNav"),
            NavigationSection.Settings => LocalizationCatalog.Get("SettingsNav"),
            _ => LocalizationCatalog.Get("DashboardNav")
        };
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
