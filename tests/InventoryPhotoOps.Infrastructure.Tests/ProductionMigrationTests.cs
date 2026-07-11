using InventoryPhotoOps.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryPhotoOps.Infrastructure.Tests;

public sealed class ProductionMigrationTests
{
    [Fact]
    public async Task SqliteMigrations_CreateProductionFoundationTables()
    {
        using var temp = new TempDirectory();
        var dbPath = Path.Combine(temp.Path, "inventory.sqlite");
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        await using (var db = new InventoryDbContext(options))
        {
            await db.Database.MigrateAsync();
            var tables = await db.Database.SqlQueryRaw<string>("select name as Value from sqlite_master where type = 'table'").ToListAsync();
            Assert.Contains("PriceEvidence", tables);
            Assert.Contains("SaleLots", tables);
            Assert.Contains("MarketplaceListings", tables);
            Assert.Contains("OcrImageArtifacts", tables);
            Assert.Contains("EbayConnectionProfiles", tables);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InventoryPhotoOpsDbTests", Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
