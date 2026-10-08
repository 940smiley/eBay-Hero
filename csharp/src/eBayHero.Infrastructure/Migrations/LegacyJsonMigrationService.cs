using System.Text.Json;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using eBayHero.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eBayHero.Infrastructure.Migrations;

public sealed class LegacyJsonMigrationService : IJsonMigrationService
{
    public async Task<MigrationReport> MigrateAsync(MigrationOptions options, CancellationToken cancellationToken)
    {
        if (options.Apply && IsLiveInventoryPath(options.DatabasePath) && !options.AllowLiveInventoryAccess)
        {
            throw new InvalidOperationException("Refusing to write to the live operations root without --allow-live.");
        }

        if (!File.Exists(options.JsonPath))
        {
            throw new FileNotFoundException("Legacy JSON catalog was not found.", options.JsonPath);
        }

        var warnings = new List<string>();
        var jsonText = await File.ReadAllTextAsync(options.JsonPath, cancellationToken);
        using var document = JsonDocument.Parse(jsonText);
        var photos = ReadArray(document.RootElement, "Photos").ToList();
        var groups = ReadArray(document.RootElement, "Groups").ToList();

        var missingFileDetails = BuildMissingFileDetails(photos);
        var missingFiles = missingFileDetails.Count;
        if (missingFiles > 0)
        {
            warnings.Add($"{missingFiles} migrated photo path(s) are currently missing.");
        }

        if (!options.Apply)
        {
            return new MigrationReport(
                Applied: false,
                PhotosRead: photos.Count,
                PhotosInserted: photos.Count,
                PhotosUpdated: 0,
                ItemsRead: groups.Count,
                ItemsInserted: groups.Count + photos.Count(p => string.IsNullOrWhiteSpace(GetString(p, "GroupId"))),
                LinksInserted: photos.Count,
                TagsInserted: CountDistinctTags(photos, groups),
                MissingFiles: missingFiles,
                BackupPath: string.Empty,
                ReportPath: string.Empty,
                Warnings: warnings)
            {
                MissingFileDetails = missingFileDetails
            };
        }

        Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);
        var dbOptions = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={options.DatabasePath}")
            .Options;
        await using var db = new InventoryDbContext(dbOptions);
        await db.Database.MigrateAsync(cancellationToken);

        var backupPath = CreateJsonBackup(options.JsonPath);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var insertedPhotos = 0;
        var updatedPhotos = 0;
        var insertedItems = 0;
        var insertedLinks = 0;
        var insertedTags = 0;
        var pathMatchedPhotos = 0;

        var tagByName = await db.Tags.ToDictionaryAsync(t => t.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var photoById = await db.Photos.ToDictionaryAsync(p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var photoByPath = new Dictionary<string, Photo>(StringComparer.OrdinalIgnoreCase);
        foreach (var existingPhoto in await db.Photos
            .Where(p => !string.IsNullOrWhiteSpace(p.FullPath))
            .ToListAsync(cancellationToken))
        {
            photoByPath.TryAdd(existingPhoto.FullPath, existingPhoto);
        }

        var migratedPhotoIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var item = MapGroup(group);
            var existing = await db.InventoryItems.FindAsync([item.Id], cancellationToken);
            if (existing is null)
            {
                db.InventoryItems.Add(item);
                insertedItems++;
            }
            else
            {
                CopyItemValues(item, existing);
            }

            foreach (var tagName in ReadStringArray(group, "Tags"))
            {
                var tag = await GetOrCreateTagAsync(db, tagByName, tagName, cancellationToken);
                if (tag.CreatedUtc > DateTimeOffset.UtcNow.AddMinutes(-1)) insertedTags++;
                if (!await db.ItemTags.AnyAsync(x => x.InventoryItemId == item.Id && x.TagId == tag.Id, cancellationToken))
                {
                    db.ItemTags.Add(new ItemTag { InventoryItemId = item.Id, TagId = tag.Id });
                }
            }
        }

        var photosByGroup = photos
            .Select((element, index) => new { Element = element, Index = index, GroupId = GetString(element, "GroupId") })
            .GroupBy(x => string.IsNullOrWhiteSpace(x.GroupId) ? "photo:" + GetString(x.Element, "Id") : x.GroupId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => GetString(x.Element, "Path")).ThenBy(x => x.Index).ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var photoElement in photos)
        {
            var photo = MapPhoto(photoElement);
            var hasExistingId = photoById.TryGetValue(photo.Id, out var existingById);
            Photo? existingByPath = null;
            var hasExistingPath = !string.IsNullOrWhiteSpace(photo.FullPath) && photoByPath.TryGetValue(photo.FullPath, out existingByPath);
            var existing = ResolveExistingPhoto(existingById, existingByPath);
            if (existing is null)
            {
                db.Photos.Add(photo);
                photoById[photo.Id] = photo;
                if (!string.IsNullOrWhiteSpace(photo.FullPath))
                {
                    photoByPath[photo.FullPath] = photo;
                }

                migratedPhotoIds[photo.Id] = photo.Id;
                insertedPhotos++;
            }
            else
            {
                CopyPhotoValues(photo, existing);
                photoById[existing.Id] = existing;
                if (!string.IsNullOrWhiteSpace(photo.FullPath))
                {
                    photoByPath[photo.FullPath] = existing;
                }

                if (!hasExistingId && hasExistingPath)
                {
                    pathMatchedPhotos++;
                }

                migratedPhotoIds[photo.Id] = existing.Id;
                updatedPhotos++;
            }

            foreach (var tagName in ReadStringArray(photoElement, "Tags"))
            {
                var tag = await GetOrCreateTagAsync(db, tagByName, tagName, cancellationToken);
                if (tag.CreatedUtc > DateTimeOffset.UtcNow.AddMinutes(-1)) insertedTags++;
                var actualPhotoId = migratedPhotoIds[photo.Id];
                if (!await db.PhotoTags.AnyAsync(x => x.PhotoId == actualPhotoId && x.TagId == tag.Id, cancellationToken))
                {
                    db.PhotoTags.Add(new PhotoTag { PhotoId = actualPhotoId, TagId = tag.Id });
                }
            }
        }

        if (pathMatchedPhotos > 0)
        {
            warnings.Add($"Reused {pathMatchedPhotos} existing photo row(s) by FullPath when legacy IDs differed.");
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var bucket in photosByGroup)
        {
            var itemId = bucket.Key.StartsWith("photo:", StringComparison.OrdinalIgnoreCase)
                ? bucket.Key
                : bucket.Key;

            if (bucket.Key.StartsWith("photo:", StringComparison.OrdinalIgnoreCase))
            {
                var photoElement = bucket.Value[0].Element;
                if (await db.InventoryItems.FindAsync([itemId], cancellationToken) is null)
                {
                    db.InventoryItems.Add(MapUngroupedPhotoItem(photoElement, itemId));
                    insertedItems++;
                }
            }

            for (var i = 0; i < bucket.Value.Count; i++)
            {
                var legacyPhotoId = GetString(bucket.Value[i].Element, "Id");
                if (string.IsNullOrWhiteSpace(legacyPhotoId) || !migratedPhotoIds.TryGetValue(legacyPhotoId, out var photoId))
                {
                    continue;
                }

                if (!await db.PhotoItemLinks.AnyAsync(x => x.PhotoId == photoId && x.InventoryItemId == itemId, cancellationToken))
                {
                    db.PhotoItemLinks.Add(new PhotoItemLink
                    {
                        PhotoId = photoId,
                        InventoryItemId = itemId,
                        SortOrder = i + 1,
                        IsPrimary = i == 0,
                        ViewType = ParseViewType(GetString(bucket.Value[i].Element, "ViewType"))
                    });
                    insertedLinks++;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var report = new MigrationReport(
            Applied: true,
            PhotosRead: photos.Count,
            PhotosInserted: insertedPhotos,
            PhotosUpdated: updatedPhotos,
            ItemsRead: groups.Count,
            ItemsInserted: insertedItems,
            LinksInserted: insertedLinks,
            TagsInserted: insertedTags,
            MissingFiles: missingFiles,
            BackupPath: backupPath,
            ReportPath: string.Empty,
            Warnings: warnings)
        {
            MissingFileDetails = missingFileDetails
        };

        var reportPath = await WriteReportAsync(options.JsonPath, report, cancellationToken);
        return report with { ReportPath = reportPath };
    }

    private static bool IsLiveInventoryPath(string path) =>
        Path.GetFullPath(path).StartsWith(@"D:\INVENTORY_PHOTO_OPS", StringComparison.OrdinalIgnoreCase);

    private static string CreateJsonBackup(string jsonPath)
    {
        var backupDir = Path.Combine(Path.GetDirectoryName(jsonPath)!, "migration-backups");
        Directory.CreateDirectory(backupDir);
        var backupPath = Path.Combine(backupDir, $"inventory-index.{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fffffff}.{Guid.NewGuid():N}.json");
        File.Copy(jsonPath, backupPath, overwrite: false);
        return backupPath;
    }

    private static async Task<string> WriteReportAsync(string jsonPath, MigrationReport report, CancellationToken cancellationToken)
    {
        var reportDir = Path.Combine(Path.GetDirectoryName(jsonPath)!, "migration-reports");
        Directory.CreateDirectory(reportDir);
        var reportPath = Path.Combine(reportDir, $"migration-report.{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.json");
        var reportWithPath = report with { ReportPath = reportPath };
        var json = JsonSerializer.Serialize(reportWithPath, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(reportPath, json, cancellationToken);
        return reportPath;
    }

    private static IEnumerable<JsonElement> ReadArray(JsonElement root, string property)
    {
        if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in value.EnumerateArray())
            {
                yield return element;
            }
        }
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value
            .EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int CountDistinctTags(IEnumerable<JsonElement> photos, IEnumerable<JsonElement> groups) =>
        photos.Concat(groups)
            .SelectMany(x => ReadStringArray(x, "Tags"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    private static IReadOnlyList<MigrationMissingFile> BuildMissingFileDetails(IEnumerable<JsonElement> photos)
    {
        var details = new List<MigrationMissingFile>();
        foreach (var photo in photos)
        {
            var path = GetString(photo, "Path");
            if (File.Exists(path))
            {
                continue;
            }

            details.Add(new MigrationMissingFile(
                PhotoId: FirstNonEmpty(GetString(photo, "Id"), "(missing id)"),
                FullPath: path,
                GroupId: GetString(photo, "GroupId"),
                FileName: GetFileNameForReport(path),
                CandidatePaths: FindCandidatePaths(path)));
        }

        return details;
    }

    private static string GetFileNameForReport(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFileName(path);
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }

    private static IReadOnlyList<string> FindCandidatePaths(string missingPath)
    {
        if (string.IsNullOrWhiteSpace(missingPath))
        {
            return [];
        }

        string fileName;
        string? parentDirectory;
        try
        {
            fileName = Path.GetFileName(missingPath);
            parentDirectory = Path.GetDirectoryName(missingPath);
        }
        catch (ArgumentException)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return [];
        }

        var searchRoot = FindNearestExistingDirectory(parentDirectory);
        if (string.IsNullOrWhiteSpace(searchRoot))
        {
            return [];
        }

        try
        {
            var fullMissingPath = Path.GetFullPath(missingPath);
            return Directory
                .EnumerateFiles(searchRoot, fileName, SearchOption.AllDirectories)
                .Where(candidate => !Path.GetFullPath(candidate).Equals(fullMissingPath, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or ArgumentException or PathTooLongException or DirectoryNotFoundException)
        {
            return [];
        }
    }

    private static string FindNearestExistingDirectory(string? directory)
    {
        while (!string.IsNullOrWhiteSpace(directory))
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    return directory;
                }

                var parent = Directory.GetParent(directory);
                if (parent is null)
                {
                    return string.Empty;
                }

                directory = parent.FullName;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or ArgumentException or PathTooLongException)
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    private static string GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private static double GetDouble(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.TryGetDouble(out var result))
        {
            return result;
        }

        return 0;
    }

    private static DateTimeOffset GetDate(JsonElement element, string property)
    {
        var text = GetString(element, property);
        return DateTimeOffset.TryParse(text, out var value) ? value.ToUniversalTime() : DateTimeOffset.UtcNow;
    }

    private static Photo MapPhoto(JsonElement element)
    {
        var path = GetString(element, "Path");
        var fileInfo = File.Exists(path) ? new FileInfo(path) : null;
        return new Photo
        {
            Id = FirstNonEmpty(GetString(element, "Id"), Guid.NewGuid().ToString("D")),
            FullPath = path,
            OriginalPath = GetString(element, "ImportedFrom"),
            FileName = Path.GetFileName(path),
            Extension = Path.GetExtension(path).ToLowerInvariant(),
            FileSize = fileInfo?.Length ?? 0,
            CreatedUtc = GetDate(element, "AddedUtc"),
            ModifiedUtc = GetDate(element, "LastWriteUtc"),
            ImportedUtc = GetDate(element, "AddedUtc"),
            Sha256 = GetString(element, "Hash"),
            IsMissing = !File.Exists(path),
            OcrText = GetString(element, "OcrText"),
            OcrConfidence = GetDouble(element, "OcrConfidence"),
            OcrProfile = ParseOcrProfile(GetString(element, "OcrMode")),
            OcrReviewed = string.Equals(GetString(element, "OcrReviewed"), "true", StringComparison.OrdinalIgnoreCase),
            ViewType = ParseViewType(GetString(element, "ViewType")),
            Notes = GetString(element, "Notes")
        };
    }

    private static InventoryItem MapGroup(JsonElement element)
    {
        var now = DateTimeOffset.UtcNow;
        return new InventoryItem
        {
            Id = FirstNonEmpty(GetString(element, "Id"), Guid.NewGuid().ToString("D")),
            Name = FirstNonEmpty(GetString(element, "Name"), "Migrated Item"),
            Category = GetString(element, "Category"),
            SportOrGame = GetString(element, "SportGame"),
            PlayerOrTitle = GetString(element, "PlayerTitle"),
            Year = GetString(element, "Year"),
            Brand = GetString(element, "Brand"),
            SetName = GetString(element, "SetName"),
            SerialNumber = GetString(element, "SerialNumber"),
            ListingStatus = ParseStatus(GetString(element, "Status")),
            Notes = GetString(element, "Notes"),
            CreatedUtc = GetDate(element, "CreatedUtc"),
            ModifiedUtc = now
        };
    }

    private static InventoryItem MapUngroupedPhotoItem(JsonElement photo, string itemId)
    {
        var fileName = Path.GetFileNameWithoutExtension(GetString(photo, "Path"));
        var title = FirstNonEmpty(GetString(photo, "PlayerTitle"), fileName, "Ungrouped Photo");
        return new InventoryItem
        {
            Id = itemId,
            Name = title,
            Category = GetString(photo, "Category"),
            SportOrGame = GetString(photo, "SportGame"),
            PlayerOrTitle = GetString(photo, "PlayerTitle"),
            Year = GetString(photo, "Year"),
            Brand = GetString(photo, "Brand"),
            SetName = GetString(photo, "SetName"),
            CardNumber = GetString(photo, "CardNumber"),
            SerialNumber = GetString(photo, "SerialNumber"),
            ISBN = GetString(photo, "ISBN"),
            Author = GetString(photo, "Author"),
            ListingStatus = ParseStatus(GetString(photo, "Status")),
            Notes = GetString(photo, "Notes")
        };
    }

    private static void CopyPhotoValues(Photo source, Photo target)
    {
        target.FullPath = source.FullPath;
        target.OriginalPath = source.OriginalPath;
        target.FileName = source.FileName;
        target.Extension = source.Extension;
        target.FileSize = source.FileSize;
        target.ModifiedUtc = source.ModifiedUtc;
        target.Sha256 = source.Sha256;
        target.IsMissing = source.IsMissing;
        target.OcrText = source.OcrText;
        target.OcrConfidence = source.OcrConfidence;
        target.OcrProfile = source.OcrProfile;
        target.OcrReviewed = source.OcrReviewed;
        target.ViewType = source.ViewType;
        target.Notes = source.Notes;
    }

    private static Photo? ResolveExistingPhoto(Photo? existingById, Photo? existingByPath)
    {
        if (existingByPath is not null && (existingById is null || !existingById.Id.Equals(existingByPath.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return existingByPath;
        }

        return existingById;
    }

    private static void CopyItemValues(InventoryItem source, InventoryItem target)
    {
        target.Name = source.Name;
        target.Category = source.Category;
        target.SportOrGame = source.SportOrGame;
        target.PlayerOrTitle = source.PlayerOrTitle;
        target.Year = source.Year;
        target.Brand = source.Brand;
        target.SetName = source.SetName;
        target.SerialNumber = source.SerialNumber;
        target.ListingStatus = source.ListingStatus;
        target.Notes = source.Notes;
        target.ModifiedUtc = DateTimeOffset.UtcNow;
    }

    private static async Task<Tag> GetOrCreateTagAsync(
        InventoryDbContext db,
        Dictionary<string, Tag> tagByName,
        string tagName,
        CancellationToken cancellationToken)
    {
        var normalized = tagName.Trim();
        if (tagByName.TryGetValue(normalized, out var existing))
        {
            return existing;
        }

        var tag = new Tag { Name = normalized };
        db.Tags.Add(tag);
        tagByName[normalized] = tag;
        await db.SaveChangesAsync(cancellationToken);
        return tag;
    }

    private static ListingStatus ParseStatus(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "not listed" or "notlisted" => ListingStatus.NotListed,
            "ready to list" or "readytolist" => ListingStatus.ReadyToList,
            "drafted" => ListingStatus.Drafted,
            "currently listed" or "currentlylisted" => ListingStatus.CurrentlyListed,
            "sold" => ListingStatus.Sold,
            "ended" => ListingStatus.Ended,
            "listed" => ListingStatus.Listed,
            "archived" => ListingStatus.Archived,
            _ => ListingStatus.NotListed
        };

    private static PhotoViewType ParseViewType(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "front" => PhotoViewType.Front,
            "back" or "rear" => PhotoViewType.Back,
            "top" => PhotoViewType.Top,
            "bottom" => PhotoViewType.Bottom,
            "left" => PhotoViewType.Left,
            "right" => PhotoViewType.Right,
            "spine" => PhotoViewType.Spine,
            "copyright page" or "copyrightpage" => PhotoViewType.CopyrightPage,
            "serial number" or "serialnumber" => PhotoViewType.SerialNumber,
            "card number" or "cardnumber" => PhotoViewType.CardNumber,
            "condition closeup" or "conditioncloseup" => PhotoViewType.ConditionCloseup,
            "other" => PhotoViewType.Other,
            _ => PhotoViewType.Unknown
        };

    private static OcrProfile ParseOcrProfile(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "trading card front" or "tradingcardfront" => OcrProfile.TradingCardFront,
            "trading card back" or "tradingcardback" => OcrProfile.TradingCardBack,
            "book cover" or "bookcover" => OcrProfile.BookCover,
            "nameplate / title only" or "nameplate/titleonly" or "nameplatetitle" => OcrProfile.NameplateTitle,
            "serial / card number" or "serialnumber" => OcrProfile.SerialNumber,
            "full image" or "fullimage" => OcrProfile.FullImage,
            _ => OcrProfile.Auto
        };

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}

