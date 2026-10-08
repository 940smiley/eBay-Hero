using eBayHero.Core.Models;
using eBayHero.Core.Services;
using eBayHero.Infrastructure.Data;
using eBayHero.Infrastructure.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eBayHero.Infrastructure.Tests;

public sealed class CardOpsImportTests
{
    [Fact]
    public async Task CardOpsImport_DryRun_ReportsCountsWithoutCreatingTarget()
    {
        using var fixture = new CardOpsFixture();
        var dbPath = Path.Combine(fixture.Root, "target.sqlite");
        var service = new CardOpsImportService();

        var report = await service.ImportAsync(
            new CardOpsImportOptions(fixture.CardOpsDbPath, dbPath, Apply: false, AllowLiveInventoryAccess: false),
            CancellationToken.None);

        Assert.False(report.Applied);
        Assert.Equal(1, report.CardsRead);
        Assert.Equal(1, report.ImagesRead);
        Assert.Equal(1, report.SourceRootsRead);
        Assert.False(File.Exists(dbPath));
    }

    [Fact]
    public async Task CardOpsImport_Apply_IsIdempotentAndPreservesCardFields()
    {
        using var fixture = new CardOpsFixture();
        var dbPath = Path.Combine(fixture.Root, "target.sqlite");
        var service = new CardOpsImportService();

        var first = await service.ImportAsync(
            new CardOpsImportOptions(fixture.CardOpsDbPath, dbPath, Apply: true, AllowLiveInventoryAccess: false),
            CancellationToken.None);
        var second = await service.ImportAsync(
            new CardOpsImportOptions(fixture.CardOpsDbPath, dbPath, Apply: true, AllowLiveInventoryAccess: false),
            CancellationToken.None);
        var dryRunAfterApply = await service.ImportAsync(
            new CardOpsImportOptions(fixture.CardOpsDbPath, dbPath, Apply: false, AllowLiveInventoryAccess: false),
            CancellationToken.None);

        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        await using var db = new InventoryDbContext(options);
        var item = await db.InventoryItems.FindAsync("cardops-card-card-1");
        var photo = await db.Photos.FindAsync("cardops-image-image-1");

        Assert.True(first.Applied);
        Assert.Equal(1, first.CardsInserted);
        Assert.Equal(1, first.ImagesInserted);
        Assert.Equal(1, first.LinksInserted);
        Assert.Equal(0, second.CardsInserted);
        Assert.Equal(1, second.CardsUpdated);
        Assert.Equal(0, dryRunAfterApply.CardsInserted);
        Assert.Equal(1, dryRunAfterApply.CardsUpdated);
        Assert.Equal(0, dryRunAfterApply.ImagesInserted);
        Assert.Equal(1, dryRunAfterApply.ImagesUpdated);
        Assert.Equal(0, dryRunAfterApply.LinksInserted);
        Assert.NotNull(item);
        Assert.Equal("Ruben Amaro", item!.PlayerOrTitle);
        Assert.Equal("1956", item.Year);
        Assert.Equal("12", item.CardNumber);
        Assert.True(item.Rookie);
        Assert.NotNull(photo);
        Assert.True(await db.PhotoItemLinks.AnyAsync(link => link.PhotoId == "cardops-image-image-1" && link.InventoryItemId == "cardops-card-card-1"));
        Assert.True(await db.CustomFieldValues.AnyAsync(field => field.InventoryItemId == "cardops-card-card-1" && field.FieldName == "CardOps.InternalSku" && field.FieldValue == "SKU-1"));
    }

    [Fact]
    public async Task CardOpsImport_Apply_UsesExistingPathRecordsWhenPresent()
    {
        using var fixture = new CardOpsFixture();
        var dbPath = Path.Combine(fixture.Root, "target.sqlite");
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        await using (var db = new InventoryDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.SourceRoots.Add(new SourceRoot { Id = "existing-root", Path = fixture.Root });
            db.Photos.Add(new Photo
            {
                Id = "existing-photo",
                FullPath = fixture.ImagePath,
                OriginalPath = fixture.ImagePath,
                FileName = "front.jpg",
                Extension = ".jpg",
                FileSize = 5
            });
            await db.SaveChangesAsync();
        }

        var service = new CardOpsImportService();
        var report = await service.ImportAsync(
            new CardOpsImportOptions(fixture.CardOpsDbPath, dbPath, Apply: true, AllowLiveInventoryAccess: false),
            CancellationToken.None);

        await using var verify = new InventoryDbContext(options);

        Assert.True(report.Applied);
        Assert.Equal(0, report.SourceRootsInserted);
        Assert.Equal(0, report.ImagesInserted);
        Assert.Equal(1, report.ImagesUpdated);
        Assert.Null(await verify.Photos.FindAsync("cardops-image-image-1"));
        Assert.True(await verify.PhotoItemLinks.AnyAsync(link => link.PhotoId == "existing-photo" && link.InventoryItemId == "cardops-card-card-1"));
    }

    private sealed class CardOpsFixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "ipo-cardops-tests-" + Guid.NewGuid().ToString("N"));
        public string CardOpsDbPath => Path.Combine(Root, "cardops.db");
        public string ImagePath => Path.Combine(Root, "front.jpg");

        public CardOpsFixture()
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(ImagePath, "image");
            CreateCardOpsDatabase();
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private void CreateCardOpsDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={CardOpsDbPath}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                create table card_instances (
                    id text primary key,
                    internal_sku text not null,
                    sport text,
                    player text,
                    team text,
                    manufacturer text,
                    brand text,
                    set_name text,
                    set_year integer,
                    card_number text,
                    subset text,
                    variation text,
                    parallel text,
                    rookie integer not null,
                    autograph integer not null,
                    relic integer not null,
                    serial_number_current integer,
                    serial_number_total integer,
                    raw_or_graded text not null,
                    grading_company text,
                    grade text,
                    quantity integer not null,
                    condition_notes text,
                    acquisition_cost text,
                    estimated_value text,
                    verified_sale_low text,
                    verified_sale_high text,
                    storage_location text,
                    current_lot_assignment text,
                    current_ebay_listing text,
                    processing_status text not null,
                    confidence real,
                    tags text not null,
                    created_at text not null,
                    updated_at text not null
                );
                create table image_assets (
                    id text primary key,
                    directory_id text not null,
                    absolute_path text not null,
                    relative_path text not null,
                    file_name text not null,
                    extension text not null,
                    file_size integer not null,
                    created_time text,
                    modified_time text,
                    sha256 text,
                    perceptual_hash text,
                    width integer,
                    height integer,
                    thumbnail_path text,
                    imported_at text not null,
                    processing_status text not null,
                    duplicate_status text not null,
                    front_back_assignment text,
                    original_location text not null,
                    card_instance_id text,
                    error_message text
                );
                create table directory_roots (
                    id text primary key,
                    path text not null,
                    label text,
                    recursive integer not null,
                    exclude_patterns text not null,
                    created_at text not null,
                    revoked_at text
                );
                insert into card_instances values (
                    'card-1', 'SKU-1', 'Baseball', 'Ruben Amaro', 'Phillies', 'Topps', 'Topps',
                    'Topps', 1956, '12', 'Base', 'White back', 'Gold', 1, 0, 0, 1, 99,
                    'raw', '', '', 1, 'Excellent corners', '2.00', '15.00', '10.00', '20.00',
                    'Box A', 'lot-a', '', 'reviewed', 0.91, '["Trading Cards","Baseball"]',
                    '2026-01-01T00:00:00Z', '2026-01-02T00:00:00Z'
                );
                insert into directory_roots values (
                    'root-1', @rootPath, 'fixture root', 1, '[]', '2026-01-01T00:00:00Z', null
                );
                insert into image_assets values (
                    'image-1', 'root-1', @imagePath, 'front.jpg', 'front.jpg', '.jpg', 5,
                    '2026-01-01T00:00:00Z', '2026-01-02T00:00:00Z',
                    'abc123', 'ffff', 600, 800, '', '2026-01-03T00:00:00Z',
                    'ready', 'unique', 'front', @imagePath, 'card-1', ''
                );
                """;
            command.Parameters.AddWithValue("@rootPath", Root);
            command.Parameters.AddWithValue("@imagePath", ImagePath);
            command.ExecuteNonQuery();
        }
    }
}

