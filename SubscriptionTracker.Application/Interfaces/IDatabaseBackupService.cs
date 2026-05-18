namespace SubscriptionTracker.Application.Interfaces;

public interface IDatabaseBackupService
{
    Task CreateBackupAsync(string destinationFilePath, CancellationToken cancellationToken = default);

    Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}
