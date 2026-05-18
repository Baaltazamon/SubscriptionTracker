using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Services;
using SubscriptionTracker.Wpf.Services;
using Microsoft.Win32;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IDatabaseBackupService _databaseBackupService;
    private readonly INotificationService _notificationService;
    private readonly IApplicationLifecycleService _applicationLifecycleService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly AppEventBus _eventBus;
    private IReadOnlyList<OptionItem<string>> _languageOptions = [];
    private IReadOnlyList<OptionItem<string>> _currencyOptions = [];
    private IReadOnlyList<OptionItem<int>> _reminderIntervalOptions = [];
    private OptionItem<string>? _selectedLanguageOption;
    private OptionItem<string>? _selectedCurrencyOption;
    private OptionItem<int>? _selectedReminderIntervalOption;
    private bool _areNotificationsEnabled;

    public SettingsViewModel(
        IAppSettingsService appSettingsService,
        IDatabaseBackupService databaseBackupService,
        INotificationService notificationService,
        IApplicationLifecycleService applicationLifecycleService,
        IThemeService themeService,
        ILocalizationService localizationService,
        AppEventBus eventBus)
    {
        _appSettingsService = appSettingsService;
        _databaseBackupService = databaseBackupService;
        _notificationService = notificationService;
        _applicationLifecycleService = applicationLifecycleService;
        _themeService = themeService;
        _localizationService = localizationService;
        _eventBus = eventBus;

        SavePreferencesCommand = new AsyncRelayCommand(SavePreferencesAsync);
        CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
        UseDarkThemeCommand = new AsyncRelayCommand(() => SetThemeAsync(AppTheme.Dark));
        UseLightThemeCommand = new AsyncRelayCommand(() => SetThemeAsync(AppTheme.Light));

        _localizationService.LanguageChanged += (_, _) => RebuildOptions();
        LoadFromSettings(_appSettingsService.GetSettings());
        RebuildOptions();
    }

    public IReadOnlyList<OptionItem<string>> LanguageOptions
    {
        get => _languageOptions;
        private set => SetProperty(ref _languageOptions, value);
    }

    public IReadOnlyList<OptionItem<string>> CurrencyOptions
    {
        get => _currencyOptions;
        private set => SetProperty(ref _currencyOptions, value);
    }

    public IReadOnlyList<OptionItem<int>> ReminderIntervalOptions
    {
        get => _reminderIntervalOptions;
        private set => SetProperty(ref _reminderIntervalOptions, value);
    }

    public OptionItem<string>? SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set => SetProperty(ref _selectedLanguageOption, value);
    }

    public OptionItem<string>? SelectedCurrencyOption
    {
        get => _selectedCurrencyOption;
        set => SetProperty(ref _selectedCurrencyOption, value);
    }

    public OptionItem<int>? SelectedReminderIntervalOption
    {
        get => _selectedReminderIntervalOption;
        set => SetProperty(ref _selectedReminderIntervalOption, value);
    }

    public bool AreNotificationsEnabled
    {
        get => _areNotificationsEnabled;
        set => SetProperty(ref _areNotificationsEnabled, value);
    }

    public string DatabasePath => _appSettingsService.GetSettings().DatabasePath;

    public string ThemeName => _themeService.CurrentTheme == AppTheme.Dark
        ? LocalizationCatalog.Get("ThemeDark")
        : LocalizationCatalog.Get("ThemeLight");

    public string StorageMode => LocalizationCatalog.Get("StorageMode");

    public string BackupStatus => LocalizationCatalog.Get("BackupStatus");

    public string AppVersion => LocalizationCatalog.Get("AppVersion");

    public string DataProfileText => LocalizationCatalog.Get("DataProfileText");

    public AsyncRelayCommand SavePreferencesCommand { get; }

    public AsyncRelayCommand CreateBackupCommand { get; }

    public AsyncRelayCommand RestoreBackupCommand { get; }

    public AsyncRelayCommand UseDarkThemeCommand { get; }

    public AsyncRelayCommand UseLightThemeCommand { get; }

    private async Task SavePreferencesAsync()
    {
        var current = _appSettingsService.GetSettings();
        var updated = current with
        {
            BaseCurrency = SelectedCurrencyOption?.Value ?? current.BaseCurrency,
            LanguageCode = SelectedLanguageOption?.Value ?? current.LanguageCode,
            ReminderCheckIntervalMinutes = SelectedReminderIntervalOption?.Value ?? current.ReminderCheckIntervalMinutes,
            NotificationsEnabled = AreNotificationsEnabled
        };

        await _appSettingsService.SaveAsync(updated);
        _localizationService.ApplyLanguage(updated.LanguageCode);
        _eventBus.PublishSettingsChanged();
        RaiseReadonlyProperties();
    }

    private async Task SetThemeAsync(AppTheme theme)
    {
        _themeService.Apply(theme);
        RaisePropertyChanged(nameof(ThemeName));

        var current = _appSettingsService.GetSettings();
        await _appSettingsService.SaveAsync(current with
        {
            Theme = theme == AppTheme.Dark ? "Dark" : "Light"
        });
    }

    private async Task CreateBackupAsync()
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"subscription_tracker_backup_{DateTime.Now:yyyy-MM-dd_HH-mm}.sqlite",
            DefaultExt = ".sqlite",
            Filter = LocalizationCatalog.Get("BackupFileDialogFilter")
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _databaseBackupService.CreateBackupAsync(dialog.FileName);
            _notificationService.ShowInfo(
                LocalizationCatalog.Format("BackupCreatedMessage", dialog.FileName),
                LocalizationCatalog.Get("BackupCreatedTitle"));
        }
        catch (Exception exception)
        {
            _notificationService.ShowError(exception.Message, LocalizationCatalog.Get("BackupFailedTitle"));
        }
    }

    private async Task RestoreBackupAsync()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = LocalizationCatalog.Get("BackupFileDialogFilter")
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (!_notificationService.Confirm(
                LocalizationCatalog.Get("BackupRestoreConfirmMessage"),
                LocalizationCatalog.Get("BackupRestoreConfirmTitle")))
        {
            return;
        }

        try
        {
            await _databaseBackupService.RestoreBackupAsync(dialog.FileName);
            _notificationService.ShowInfo(
                LocalizationCatalog.Get("BackupRestoredMessage"),
                LocalizationCatalog.Get("BackupRestoredTitle"));
            _applicationLifecycleService.Restart();
        }
        catch (Exception exception)
        {
            _notificationService.ShowError(exception.Message, LocalizationCatalog.Get("BackupFailedTitle"));
        }
    }

    private void LoadFromSettings(AppSettingsDto settings)
    {
        AreNotificationsEnabled = settings.NotificationsEnabled;
    }

    private void RebuildOptions()
    {
        var settings = _appSettingsService.GetSettings();

        LanguageOptions =
        [
            new OptionItem<string> { Value = "ru-RU", Label = LocalizationCatalog.Get("LanguageRussian") },
            new OptionItem<string> { Value = "en-US", Label = LocalizationCatalog.Get("LanguageEnglish") }
        ];

        CurrencyOptions = CurrencyConverter.GetSupportedCurrencies()
            .Select(currency => new OptionItem<string> { Value = currency, Label = currency })
            .ToArray();

        ReminderIntervalOptions =
        [
            new OptionItem<int> { Value = 15, Label = LocalizationCatalog.Format("ReminderCheckIntervalFormat", 15) },
            new OptionItem<int> { Value = 30, Label = LocalizationCatalog.Format("ReminderCheckIntervalFormat", 30) },
            new OptionItem<int> { Value = 60, Label = LocalizationCatalog.Format("ReminderCheckIntervalFormat", 60) },
            new OptionItem<int> { Value = 180, Label = LocalizationCatalog.Format("ReminderCheckIntervalFormat", 180) }
        ];

        SelectedLanguageOption = LanguageOptions.FirstOrDefault(item => item.Value == settings.LanguageCode) ?? LanguageOptions.First();
        SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(item => item.Value == settings.BaseCurrency) ?? CurrencyOptions.First();
        SelectedReminderIntervalOption = ReminderIntervalOptions.FirstOrDefault(item => item.Value == settings.ReminderCheckIntervalMinutes) ?? ReminderIntervalOptions[2];
        AreNotificationsEnabled = settings.NotificationsEnabled;

        RaiseReadonlyProperties();
    }

    private void RaiseReadonlyProperties()
    {
        RaisePropertyChanged(nameof(DatabasePath));
        RaisePropertyChanged(nameof(ThemeName));
        RaisePropertyChanged(nameof(StorageMode));
        RaisePropertyChanged(nameof(BackupStatus));
        RaisePropertyChanged(nameof(AppVersion));
        RaisePropertyChanged(nameof(DataProfileText));
    }
}
