using InventoryPhotoOps.Core.Models;
using InventoryPhotoOps.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryPhotoOps.Infrastructure.Tests;

public sealed class DateTimeOffsetSqliteTests
{
    [Fact]
    public async Task SqliteQueries_CanOrderByDateTimeOffsetColumns()
    {
        using var temp = new TempDirectory();
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={Path.Combine(temp.Path, "inventory.sqlite")}")
            .Options;

        await using (var db = new InventoryDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.Photos.AddRange(
                new Photo { Id = "older-photo", FullPath = "older.jpg", FileName = "older.jpg", ImportedUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new Photo { Id = "newer-photo", FullPath = "newer.jpg", FileName = "newer.jpg", ImportedUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero) });

            db.InventoryItems.AddRange(
                new InventoryItem { Id = "undated-item", Name = "Undated" },
                new InventoryItem { Id = "older-item", Name = "Older", DateListedUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new InventoryItem { Id = "newer-item", Name = "Newer", DateListedUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero) });

            await db.SaveChangesAsync();
        }

        await using (var db = new InventoryDbContext(options))
        {
            var photoIds = await db.Photos
                .OrderByDescending(photo => photo.ImportedUtc)
                .Select(photo => photo.Id)
                .ToListAsync();

            var itemIds = await db.InventoryItems
                .OrderByDescending(item => item.DateListedUtc)
                .Select(item => item.Id)
                .ToListAsync();

            Assert.Equal(["newer-photo", "older-photo"], photoIds);
            Assert.Equal(["newer-item", "older-item", "undated-item"], itemIds);
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
