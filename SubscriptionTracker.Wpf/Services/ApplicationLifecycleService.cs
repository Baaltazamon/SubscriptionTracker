using System.Diagnostics;
using System.Windows;

namespace SubscriptionTracker.Wpf.Services;

public sealed class ApplicationLifecycleService : IApplicationLifecycleService
{
    public void Restart()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            Process.Start(new ProcessStartInfo(processPath)
            {
                UseShellExecute = true
            });
        }

        System.Windows.Application.Current.Shutdown();
    }
}
