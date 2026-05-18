using Microsoft.Win32;

namespace SubscriptionTracker.Wpf.Services;

public sealed class AutoStartService : IAutoStartService
{
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SubscriptionTracker";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
        var currentValue = key?.GetValue(AppName) as string;
        return string.Equals(currentValue, BuildCommand(), StringComparison.Ordinal);
    }

    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true);
        if (enabled)
        {
            key.SetValue(AppName, BuildCommand(), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }

        return Task.CompletedTask;
    }

    private static string BuildCommand()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Unable to resolve the application path for startup registration.");
        }

        return $"\"{processPath}\"";
    }
}
