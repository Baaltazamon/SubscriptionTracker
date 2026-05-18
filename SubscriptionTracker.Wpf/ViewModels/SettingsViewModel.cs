using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Services;
using SubscriptionTracker.Wpf.Dialogs;
using SubscriptionTracker.Wpf.Services;
using Microsoft.Win32;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IDatabaseBackupService _databaseBackupService;
    private readonly IDialogService _dialogService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICategoryManagementDialogService _categoryManagementDialogService;
    private readonly IAutoStartService _autoStartService;
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
    private CategoryListItemDto? _selectedCategory;
    private bool _areNotificationsEnabled;
    private bool _launchOnStartup;

    public SettingsViewModel(
        IAppSettingsService appSettingsService,
        IDatabaseBackupService databaseBackupService,
        IDialogService dialogService,
        IServiceScopeFactory scopeFactory,
        ICategoryManagementDialogService categoryManagementDialogService,
        IAutoStartService autoStartService,
        IApplicationLifecycleService applicationLifecycleService,
        IThemeService themeService,
        ILocalizationService localizationService,
        AppEventBus eventBus)
    {
        _appSettingsService = appSettingsService;
        _databaseBackupService = databaseBackupService;
        _dialogService = dialogService;
        _scopeFactory = scopeFactory;
        _categoryManagementDialogService = categoryManagementDialogService;
        _autoStartService = autoStartService;
        _applicationLifecycleService = applicationLifecycleService;
        _themeService = themeService;
        _localizationService = localizationService;
        _eventBus = eventBus;

        SavePreferencesCommand = new AsyncRelayCommand(SavePreferencesAsync);
        CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
        AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync);
        EditCategoryCommand = new AsyncRelayCommand(EditCategoryAsync, () => SelectedCategory is not null);
        DeleteCategoryCommand = new AsyncRelayCommand(DeleteCategoryAsync, () => SelectedCategory is not null && !SelectedCategory.IsSystem);
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

    public ObservableCollection<CategoryListItemDto> Categories { get; } = [];

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

    public bool LaunchOnStartup
    {
        get => _launchOnStartup;
        set => SetProperty(ref _launchOnStartup, value);
    }

    public CategoryListItemDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                EditCategoryCommand.NotifyCanExecuteChanged();
                DeleteCategoryCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string DatabasePath => _appSettingsService.GetSettings().DatabasePath;

    public string ThemeName => _themeService.CurrentTheme == AppTheme.Dark
        ? LocalizationCatalog.Get("ThemeDark")
        : LocalizationCatalog.Get("ThemeLight");

    public string StorageMode => LocalizationCatalog.Get("StorageMode");

    public string BackupStatus => LocalizationCatalog.Get("BackupStatus");

    public string AppVersion => LocalizationCatalog.Get("AppVersion");

    public string DataProfileText => LocalizationCatalog.Get("DataProfileText");

    public int CategoryCount => Categories.Count;

    public int CustomCategoryCount => Categories.Count(static item => !item.IsSystem);

    public int LinkedSubscriptionCount => Categories.Sum(static item => item.SubscriptionCount);

    public AsyncRelayCommand SavePreferencesCommand { get; }

    public AsyncRelayCommand CreateBackupCommand { get; }

    public AsyncRelayCommand RestoreBackupCommand { get; }

    public AsyncRelayCommand AddCategoryCommand { get; }

    public AsyncRelayCommand EditCategoryCommand { get; }

    public AsyncRelayCommand DeleteCategoryCommand { get; }

    public AsyncRelayCommand UseDarkThemeCommand { get; }

    public AsyncRelayCommand UseLightThemeCommand { get; }

    public override async Task RefreshAsync()
    {
        await LoadCategoriesAsync();
    }

    private async Task SavePreferencesAsync()
    {
        var current = _appSettingsService.GetSettings();
        var updated = current with
        {
            BaseCurrency = SelectedCurrencyOption?.Value ?? current.BaseCurrency,
            LanguageCode = SelectedLanguageOption?.Value ?? current.LanguageCode,
            ReminderCheckIntervalMinutes = SelectedReminderIntervalOption?.Value ?? current.ReminderCheckIntervalMinutes,
            NotificationsEnabled = AreNotificationsEnabled,
            LaunchOnStartup = LaunchOnStartup
        };

        await _appSettingsService.SaveAsync(updated);
        await _autoStartService.SetEnabledAsync(updated.LaunchOnStartup);
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
            _dialogService.ShowInfo(
                LocalizationCatalog.Format("BackupCreatedMessage", dialog.FileName),
                LocalizationCatalog.Get("BackupCreatedTitle"));
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("BackupFailedTitle"));
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

        if (!_dialogService.Confirm(
                LocalizationCatalog.Get("BackupRestoreConfirmMessage"),
                LocalizationCatalog.Get("BackupRestoreConfirmTitle"),
                DialogKind.Warning,
                DialogButton.Restore,
                DialogButton.Cancel))
        {
            return;
        }

        try
        {
            await _databaseBackupService.RestoreBackupAsync(dialog.FileName);
            _dialogService.ShowInfo(
                LocalizationCatalog.Get("BackupRestoredMessage"),
                LocalizationCatalog.Get("BackupRestoredTitle"));
            _applicationLifecycleService.Restart();
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("BackupFailedTitle"));
        }
    }

    private void LoadFromSettings(AppSettingsDto settings)
    {
        AreNotificationsEnabled = settings.NotificationsEnabled;
        LaunchOnStartup = _autoStartService.IsEnabled();
    }

    private async Task AddCategoryAsync()
    {
        var request = await _categoryManagementDialogService.ShowEditorAsync(null);
        if (request is null)
        {
            return;
        }

        await SaveCategoryAsync(request);
    }

    private async Task EditCategoryAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var request = await _categoryManagementDialogService.ShowEditorAsync(SelectedCategory);
        if (request is null)
        {
            return;
        }

        await SaveCategoryAsync(request);
    }

    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var request = await _categoryManagementDialogService.ShowDeleteAsync(SelectedCategory, Categories.ToArray());
        if (request is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            await categoryService.DeleteAsync(request);
            await LoadCategoriesAsync();
            _eventBus.PublishDataChanged();
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("CategoryDeleteFailedTitle"));
        }
        finally
        {
            IsBusy = false;
        }
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
        LaunchOnStartup = _autoStartService.IsEnabled();

        RaiseReadonlyProperties();
    }

    private async Task SaveCategoryAsync(SaveCategoryRequest request)
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            var savedCategory = await categoryService.SaveAsync(request);
            await LoadCategoriesAsync(savedCategory.Id);
            _eventBus.PublishDataChanged();
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message, LocalizationCatalog.Get("CategorySaveFailedTitle"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCategoriesAsync(Guid? selectedCategoryId = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
        var items = await categoryService.GetManageableAsync();

        Categories.Clear();
        foreach (var item in items)
        {
            Categories.Add(item);
        }

        var targetCategoryId = selectedCategoryId ?? SelectedCategory?.Id;
        SelectedCategory = targetCategoryId.HasValue
            ? Categories.FirstOrDefault(item => item.Id == targetCategoryId.Value)
            : Categories.FirstOrDefault();

        RaiseCategorySummaryProperties();
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

    private void RaiseCategorySummaryProperties()
    {
        RaisePropertyChanged(nameof(CategoryCount));
        RaisePropertyChanged(nameof(CustomCategoryCount));
        RaisePropertyChanged(nameof(LinkedSubscriptionCount));
        EditCategoryCommand.NotifyCanExecuteChanged();
        DeleteCategoryCommand.NotifyCanExecuteChanged();
    }
}
