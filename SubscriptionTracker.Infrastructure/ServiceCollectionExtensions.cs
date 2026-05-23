using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Infrastructure.Notifications;
using SubscriptionTracker.Infrastructure.Persistence;
using SubscriptionTracker.Infrastructure.Reports;

namespace SubscriptionTracker.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var settings = serviceProvider.GetRequiredService<IAppSettingsService>().GetSettings();
            options.UseSqlite($"Data Source={settings.DatabasePath}");
        });

        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPaymentHistoryService, PaymentHistoryService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ISubscriptionImportService, SubscriptionImportService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IExportService, ExcelExportService>();

        return services;
    }
}
