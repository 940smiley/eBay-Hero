using System.Text.Json;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using eBayHero.Infrastructure.Data;
using eBayHero.Infrastructure.Migration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eBayHero.Infrastructure.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task LegacyMigration_DryRun_DoesNotCreateDatabase()
    {
        using var fixture = new TempFixture();
        var json = fixture.WriteLegacyJson();
        var dbPath = Path.Combine(fixture.Root, "inventory.sqlite");
        var service = new LegacyJsonMigrationService();

        var report = await service.MigrateAsync(new MigrationOptions(json, dbPath, Apply: false, AllowLiveInventoryAccess: false), CancellationToken.None);

        Assert.False(report.Applied);
        Assert.Equal(2, report.PhotosRead);
        Assert.False(File.Exists(dbPath));
    }

    [Fact]
    public async Task LegacyMigration_DryRun_ReportsMissingFileDetailsAndCandidates()
    {
        using var fixture = new TempFixture();
        var missingPath = Path.Combine(fixture.Root, "missing", "front.jpg");
        var candidateDirectory = Path.Combine(fixture.Root, "candidate");
        Directory.CreateDirectory(candidateDirectory);
        var candidatePath = Path.Combine(candidateDirectory, "front.jpg");
        File.WriteAllText(candidatePath, "replacement");
        var json = fixture.WriteLegacyJson(frontPath: missingPath, createFrontFile: false);
        var dbPath = Path.Combine(fixture.Root, "inventory.sqlite");
        var service = new LegacyJsonMigrationService();

        var report = await service.MigrateAsync(new MigrationOptions(json, dbPath, Apply: false, AllowLiveInventoryAccess: false), CancellationToken.None);

        Assert.Equal(1, report.MissingFiles);
        var detail = Assert.Single(report.MissingFileDetails);
        Assert.Equal("photo-front", detail.PhotoId);
        Assert.Equal(missingPath, detail.FullPath);
        Assert.Equal("front.jpg", detail.FileName);
        Assert.Contains(candidatePath, detail.CandidatePaths);
        Assert.Contains(report.Warnings, warning => warning.Contains("currently missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LegacyMigration_Apply_IsIdempotentAndPreservesIds()
    {
        using var fixture = new TempFixture();
        var json = fixture.WriteLegacyJson();
        var dbPath = Path.Combine(fixture.Root, "inventory.sqlite");
        var service = new LegacyJsonMigrationService();

        await service.MigrateAsync(new MigrationOptions(json, dbPath, Apply: true, AllowLiveInventoryAccess: false), CancellationToken.None);
        var second = await service.MigrateAsync(new MigrationOptions(json, dbPath, Apply: true, AllowLiveInventoryAccess: false), CancellationToken.None);

        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        await using var db = new InventoryDbContext(options);
        Assert.Equal(2, await db.Photos.CountAsync());
        Assert.Equal(1, await db.InventoryItems.CountAsync(i => i.Id == "group-1"));
        Assert.Equal(2, await db.PhotoItemLinks.CountAsync(l => l.InventoryItemId == "group-1"));
        Assert.Equal(0, second.PhotosInserted);
    }

    [Fact]
    public async Task LegacyMigration_Apply_WritesReportWithReportPath()
    {
        using var fixture = new TempFixture();
        var json = fixture.WriteLegacyJson();
        var dbPath = Path.Combine(fixture.Root, "inventory.sqlite");
        var service = new LegacyJsonMigrationService();

        var report = await service.MigrateAsync(new MigrationOptions(json, dbPath, Apply: true, AllowLiveInventoryAccess: false), CancellationToken.None);

        Assert.True(File.Exists(report.ReportPath));
        using var document = JsonDocument.Parse(File.ReadAllText(report.ReportPath));
        Assert.Equal(report.ReportPath, document.RootElement.GetProperty("ReportPath").GetString());
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("MissingFileDetails").ValueKind);
    }

    [Fact]
    public async Task LegacyMigration_Apply_ReusesExistingPhotoWithSameFullPath()
    {
        using var fixture = new TempFixture();
        var json = fixture.WriteLegacyJson();
        var dbPath = Path.Combine(fixture.Root, "inventory.sqlite");
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite($"Data Source={dbPath}").Options;

        await using (var db = new InventoryDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.Photos.Add(new Photo
            {
                Id = "existing-front-photo",
                FullPath = fixture.FrontPath,
                OriginalPath = fixture.FrontPath,
                FileName = Path.GetFileName(fixture.FrontPath),
                Extension = ".jpg"
            });
            await db.SaveChangesAsync();
        }

        var service = new LegacyJsonMigrationService();
        var report = await service.MigrateAsync(new MigrationOptions(json, dbPath, Apply: true, AllowLiveInventoryAccess: false), CancellationToken.None);

        await using (var db = new InventoryDbContext(options))
        {
            Assert.Equal(2, await db.Photos.CountAsync());
            Assert.Null(await db.Photos.FindAsync("photo-front"));
            Assert.NotNull(await db.Photos.FindAsync("existing-front-photo"));
            Assert.True(await db.PhotoTags.AnyAsync(t => t.PhotoId == "existing-front-photo"));
            Assert.True(await db.PhotoItemLinks.AnyAsync(l => l.PhotoId == "existing-front-photo" && l.InventoryItemId == "group-1"));
        }

        Assert.Equal(1, report.PhotosInserted);
        Assert.Equal(1, report.PhotosUpdated);
        Assert.Contains(report.Warnings, warning => warning.Contains("Reused 1 existing photo row", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TempFixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "ipo-tests-" + Guid.NewGuid().ToString("N"));
        public string FrontPath => Path.Combine(Root, "front.jpg");
        public string BackPath => Path.Combine(Root, "back.jpg");

        public TempFixture()
        {
            Directory.CreateDirectory(Root);
        }

        public string WriteLegacyJson(string? frontPath = null, string? backPath = null, bool createFrontFile = true, bool createBackFile = true)
        {
            frontPath ??= FrontPath;
            backPath ??= BackPath;
            if (createFrontFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(frontPath)!);
                File.WriteAllText(frontPath, "front");
            }

            if (createBackFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backPath)!);
                File.WriteAllText(backPath, "back");
            }

            var jsonPath = Path.Combine(Root, "inventory-index.json");
            var payload = new
            {
                Photos = new[]
                {
                    new { Id = "photo-front", Path = frontPath, GroupId = "group-1", Status = "Not Listed", Tags = new[] { "Trading Cards" }, OcrText = "Ruben Amaro", OcrConfidence = 88 },
                    new { Id = "photo-back", Path = backPath, GroupId = "group-1", Status = "Not Listed", Tags = new[] { "Back" }, OcrText = "Card #12", OcrConfidence = 80 }
                },
                Groups = new[]
                {
                    new { Id = "group-1", Name = "1956 Topps Ruben Amaro", Status = "Not Listed", Category = "Trading Cards", SportGame = "Baseball", PlayerTitle = "Ruben Amaro", Tags = new[] { "Trading Cards" } }
                }
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload));
            return jsonPath;
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}

