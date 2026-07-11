using InventoryPhotoOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryPhotoOps.Infrastructure.Migration;

public sealed class DatabaseMaintenanceService(IDbContextFactory<InventoryDbContext> dbContextFactory)
{
    public async Task EnsureMigratedAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }

    public async Task<string> BackupAsync(string databasePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException("Database file was not found.", databasePath);
        }

        var backupDir = Path.Combine(Path.GetDirectoryName(databasePath)!, "backups");
        Directory.CreateDirectory(backupDir);
        var backupPath = Path.Combine(backupDir, $"inventory-photo-ops.{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.sqlite");
        await using var source = File.OpenRead(databasePath);
        await using var destination = File.Create(backupPath);
        await source.CopyToAsync(destination, cancellationToken);
        return backupPath;
    }

    public async Task<bool> CheckIntegrityAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
        return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
    }
}

