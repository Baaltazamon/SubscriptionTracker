namespace SubscriptionTracker.Application.Interfaces;

public interface ISubscriptionImportTemplateService
{
    Task CreateTemplateAsync(string filePath, CancellationToken cancellationToken = default);
}
