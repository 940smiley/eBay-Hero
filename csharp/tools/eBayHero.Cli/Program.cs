using System.Text.Json;
using eBayHero.Core.Configuration;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using eBayHero.Export;
using eBayHero.FileSystem;
using eBayHero.Infrastructure;
using eBayHero.Infrastructure.Data;
using eBayHero.Infrastructure.Migrations;
using eBayHero.Ocr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var parsed = ParseArgs(args);
var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.OrdinalIgnoreCase)) ?? "help";
var subcommand = args.SkipWhile(a => a != command).Skip(1).FirstOrDefault(a => !a.StartsWith("--", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
var options = new InventoryOptions();
if (parsed.TryGetValue("ops-root", out var opsRoot)) options.OperationsRoot = opsRoot;
options.DevelopmentSafeMode = !parsed.ContainsKey("allow-live");

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        if (parsed.ContainsKey("verbose"))
        {
            logging.AddSimpleConsole(options => options.SingleLine = true);
        }
    })
    .ConfigureServices(services =>
    {
        services.AddInventoryInfrastructure(options);
        services.AddInventoryFileSystem();
        services.AddInventoryOcr();
        services.AddInventoryExport();
    })
    .Build();

try
{
    var result = command.ToLowerInvariant() switch
    {
        "scan" => await ScanAsync(host.Services, parsed, options),
        "migrate" => await MigrateAsync(host.Services, parsed, options),
        "import" when subcommand.Equals("cardops", StringComparison.OrdinalIgnoreCase) => await ImportCardOpsAsync(host.Services, parsed, options),
        "ocr" => await OcrAsync(host.Services, parsed, options),
        "database" when subcommand.Equals("check", StringComparison.OrdinalIgnoreCase) => await DatabaseCheckAsync(host.Services),
        "database" when subcommand.Equals("backup", StringComparison.OrdinalIgnoreCase) => await DatabaseBackupAsync(host.Services, options),
        "export" => await ExportAsync(host.Services, parsed, options),
        _ => new { error = "Unknown command", commands = new[] { "scan", "migrate", "import cardops", "ocr", "database check", "database backup", "export" } }
    };

    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    return result.GetType().GetProperty("error") is null ? 0 : 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new { error = ex.Message, exception = ex.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
    return 1;
}

static async Task<object> ScanAsync(IServiceProvider services, Dictionary<string, string> parsed, InventoryOptions options)
{
    RequireLiveAccessIfNeeded(parsed.GetValueOrDefault("root", options.DefaultSourceRoot), options);
    RequireLiveAccessIfNeeded(options.DatabasePath, options);

    var scanner = services.GetRequiredService<IFileScanner>();
    var dbFactory = services.GetRequiredService<IDbContextFactory<InventoryDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();

    var rootPath = parsed.GetValueOrDefault("root", options.DefaultSourceRoot);
    var root = new SourceRoot { Path = rootPath, Enabled = true, Recursive = true };
    var added = 0;
    var updated = 0;
    await foreach (var scan in scanner.ScanAsync(root, options, CancellationToken.None))
    {
        var existing = await db.Photos.FirstOrDefaultAsync(p => p.FullPath == scan.NormalizedPath);
        if (existing is null)
        {
            db.Photos.Add(new Photo
            {
                FullPath = scan.NormalizedPath,
                OriginalPath = scan.FullPath,
                FileName = Path.GetFileName(scan.NormalizedPath),
                Extension = scan.Extension,
                FileSize = scan.FileSize,
                ModifiedUtc = scan.ModifiedUtc,
                ImportedUtc = DateTimeOffset.UtcNow
            });
            added++;
        }
        else
        {
            existing.FileSize = scan.FileSize;
            existing.ModifiedUtc = scan.ModifiedUtc;
            existing.IsMissing = false;
            updated++;
        }
    }

    await db.SaveChangesAsync();
    return new { command = "scan", root = rootPath, added, updated };
}

static async Task<object> MigrateAsync(IServiceProvider services, Dictionary<string, string> parsed, InventoryOptions options)
{
    var migrator = services.GetRequiredService<IJsonMigrationService>();
    return await migrator.MigrateAsync(
        new MigrationOptions(
            parsed.GetValueOrDefault("json", options.LegacyJsonPath),
            parsed.GetValueOrDefault("database", options.DatabasePath),
            parsed.ContainsKey("apply"),
            parsed.ContainsKey("allow-live")),
        CancellationToken.None);
}

static async Task<object> ImportCardOpsAsync(IServiceProvider services, Dictionary<string, string> parsed, InventoryOptions options)
{
    if (!parsed.TryGetValue("source-db", out var sourceDb))
    {
        return new { error = "--source-db is required" };
    }

    var databasePath = parsed.GetValueOrDefault("database", options.DatabasePath);
    if (parsed.ContainsKey("apply"))
    {
        RequireLiveAccessIfNeeded(databasePath, options);
    }

    var importer = services.GetRequiredService<ICardOpsImportService>();
    return await importer.ImportAsync(
        new CardOpsImportOptions(
            SourceDatabasePath: sourceDb,
            TargetDatabasePath: databasePath,
            Apply: parsed.ContainsKey("apply"),
            AllowLiveInventoryAccess: parsed.ContainsKey("allow-live")),
        CancellationToken.None);
}

static async Task<object> OcrAsync(IServiceProvider services, Dictionary<string, string> parsed, InventoryOptions options)
{
    if (!parsed.TryGetValue("photo-id", out var photoId))
    {
        return new { error = "--photo-id is required" };
    }

    var dbFactory = services.GetRequiredService<IDbContextFactory<InventoryDbContext>>();
    var ocr = services.GetRequiredService<IOcrService>();
    await using var db = await dbFactory.CreateDbContextAsync();
    var photo = await db.Photos.FindAsync(photoId);
    if (photo is null)
    {
        return new { error = "Photo was not found", photoId };
    }

    var result = await ocr.RunAsync(
        new OcrRequest(photo.Id, photo.FullPath, OcrProfile.TradingCardFront, options.OcrLanguage, options.TesseractPath, options.OcrUserWordsPath, options.OcrTempRoot),
        CancellationToken.None);

    return new { command = "ocr", photoId, result.BestText, result.AverageConfidence, candidateCount = result.Candidates.Count };
}

static async Task<object> DatabaseCheckAsync(IServiceProvider services)
{
    var options = services.GetRequiredService<InventoryOptions>();
    RequireLiveAccessIfNeeded(options.DatabasePath, options);
    var maintenance = services.GetRequiredService<DatabaseMaintenanceService>();
    return new { command = "database check", ok = await maintenance.CheckIntegrityAsync(CancellationToken.None) };
}

static async Task<object> DatabaseBackupAsync(IServiceProvider services, InventoryOptions options)
{
    RequireLiveAccessIfNeeded(options.DatabasePath, options);
    var maintenance = services.GetRequiredService<DatabaseMaintenanceService>();
    return new { command = "database backup", backupPath = await maintenance.BackupAsync(options.DatabasePath, CancellationToken.None) };
}

static async Task<object> ExportAsync(IServiceProvider services, Dictionary<string, string> parsed, InventoryOptions options)
{
    RequireLiveAccessIfNeeded(options.EbayExportRoot, options);
    var view = parsed.GetValueOrDefault("view", "Not Listed Cards");
    var dbFactory = services.GetRequiredService<IDbContextFactory<InventoryDbContext>>();
    var exporter = services.GetRequiredService<IEbayExportService>();
    await using var db = await dbFactory.CreateDbContextAsync();

    var query = db.InventoryItems.AsQueryable();
    if (view.Equals("Not Listed Cards", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(i => i.ListingStatus == ListingStatus.NotListed && i.Category.Contains("Card"));
    }
    else if (view.Equals("Ready for eBay", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(i => i.ListingStatus == ListingStatus.ReadyToList || i.ListingStatus == ListingStatus.NotListed);
    }

    var items = await query.Include(i => i.ItemTags).ThenInclude(t => t.Tag).Take(500).ToListAsync();
    var photosByItem = new Dictionary<string, IReadOnlyList<Photo>>(StringComparer.OrdinalIgnoreCase);
    foreach (var item in items)
    {
        photosByItem[item.Id] = await db.PhotoItemLinks
            .Where(l => l.InventoryItemId == item.Id)
            .Include(l => l.Photo)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.Photo!)
            .ToListAsync();
    }

    var result = await exporter.ExportAsync(new ExportRequest(options.EbayExportRoot, items, photosByItem, ListingStatus.Drafted), CancellationToken.None);
    return new { command = "export", view, result.ExportDirectory, result.ItemCount, result.PhotoCount };
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = args[i][2..];
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            result[key] = args[++i];
        }
        else
        {
            result[key] = "true";
        }
    }

    return result;
}

static void RequireLiveAccessIfNeeded(string path, InventoryOptions options)
{
    var full = Path.GetFullPath(path);
    var touchesLive = full.StartsWith(@"F:\Inventory", StringComparison.OrdinalIgnoreCase) ||
                      full.StartsWith(@"D:\INVENTORY_PHOTO_OPS", StringComparison.OrdinalIgnoreCase);
    if (touchesLive && options.DevelopmentSafeMode)
    {
        throw new InvalidOperationException("This command touches a live inventory path. Re-run with --allow-live when you intend to use live inventory data.");
    }
}

