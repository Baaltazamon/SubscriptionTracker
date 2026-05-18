using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Infrastructure;
using SubscriptionTracker.Infrastructure.Persistence;
using SubscriptionTracker.Wpf.Services;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
            })
            .ConfigureServices((_, services) =>
            {
                services.AddInfrastructure();
                services.AddSingleton<AppEventBus>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IToastNotificationService, WindowsToastNotificationService>();
                services.AddSingleton<IAutoStartService, AutoStartService>();
                services.AddSingleton<IApplicationLifecycleService, ApplicationLifecycleService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ILocalizationService, LocalizationService>();
                services.AddSingleton<ICategoryManagementDialogService, CategoryManagementDialogService>();
                services.AddSingleton<ISubscriptionEditorService, SubscriptionEditorService>();
                services.AddSingleton<ReminderScheduler>();

                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<SubscriptionsViewModel>();
                services.AddSingleton<CalendarViewModel>();
                services.AddSingleton<AnalyticsViewModel>();
                services.AddSingleton<PaymentHistoryViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<MainViewModel>();

                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        using (var scope = _host.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        }

        var settingsService = _host.Services.GetRequiredService<IAppSettingsService>();
        var localizationService = _host.Services.GetRequiredService<ILocalizationService>();
        var themeService = _host.Services.GetRequiredService<IThemeService>();
        var settings = settingsService.GetSettings();

        localizationService.ApplyLanguage(settings.LanguageCode);
        themeService.Apply(string.Equals(settings.Theme, "Light", StringComparison.OrdinalIgnoreCase) ? AppTheme.Light : AppTheme.Dark);

        var scheduler = _host.Services.GetRequiredService<ReminderScheduler>();
        await scheduler.RunStartupCheckAsync();
        scheduler.Start();

        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
