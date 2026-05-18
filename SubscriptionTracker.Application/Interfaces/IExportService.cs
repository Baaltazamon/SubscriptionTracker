namespace SubscriptionTracker.Application.Interfaces;

public interface IExportService
{
    Task ExportAsync(string filePath, CancellationToken cancellationToken = default);
}
