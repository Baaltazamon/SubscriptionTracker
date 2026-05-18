using Microsoft.Data.Sqlite;
using SubscriptionTracker.Application.Interfaces;

namespace SubscriptionTracker.Infrastructure;

public sealed class DatabaseBackupService(IAppSettingsService settingsService) : IDatabaseBackupService
{
    public Task CreateBackupAsync(string destinationFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourceFilePath = settingsService.GetSettings().DatabasePath;
        EnsureDatabaseExists(sourceFilePath);

        var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new InvalidOperationException("Destination directory is not specified.");
        }

        Directory.CreateDirectory(destinationDirectory);
        SqliteConnection.ClearAllPools();
        CopyFileSet(sourceFilePath, destinationFilePath, overwrite: true);

        return Task.CompletedTask;
    }

    public Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureDatabaseExists(backupFilePath);

        var targetFilePath = settingsService.GetSettings().DatabasePath;
        var backupFullPath = Path.GetFullPath(backupFilePath);
        var targetFullPath = Path.GetFullPath(targetFilePath);

        if (string.Equals(backupFullPath, targetFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Backup file matches the active database path.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath)!);
        SqliteConnection.ClearAllPools();
        DeleteFileSet(targetFullPath);
        CopyFileSet(backupFullPath, targetFullPath, overwrite: true);

        return Task.CompletedTask;
    }

    private static void EnsureDatabaseExists(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("SQLite database file was not found.", filePath);
        }
    }

    private static void CopyFileSet(string sourceFilePath, string destinationFilePath, bool overwrite)
    {
        foreach (var suffix in EnumerateSuffixes())
        {
            var source = sourceFilePath + suffix;
            if (!File.Exists(source))
            {
                continue;
            }

            var destination = destinationFilePath + suffix;
            File.Copy(source, destination, overwrite);
        }
    }

    private static void DeleteFileSet(string filePath)
    {
        foreach (var suffix in EnumerateSuffixes())
        {
            var candidate = filePath + suffix;
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }

    private static IEnumerable<string> EnumerateSuffixes()
    {
        yield return string.Empty;
        yield return "-wal";
        yield return "-shm";
    }
}
