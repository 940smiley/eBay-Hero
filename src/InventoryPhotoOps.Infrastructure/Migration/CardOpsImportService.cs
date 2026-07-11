using System.Globalization;
using System.Text.Json;
using InventoryPhotoOps.Core.Models;
using InventoryPhotoOps.Core.Services;
using InventoryPhotoOps.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryPhotoOps.Infrastructure.Migration;

public sealed class CardOpsImportService : ICardOpsImportService
{
    public async Task<CardOpsImportReport> ImportAsync(CardOpsImportOptions options, CancellationToken cancellationToken)
    {
        if (!File.Exists(options.SourceDatabasePath))
        {
            throw new FileNotFoundException("CardOps SQLite database was not found.", options.SourceDatabasePath);
        }

        if (options.Apply && IsLiveInventoryPath(options.TargetDatabasePath) && !options.AllowLiveInventoryAccess)
        {
            throw new InvalidOperationException("Refusing to write to the live operations root without --allow-live.");
        }

        var warnings = new List<string>();
        var cards = await ReadCardsAsync(options.SourceDatabasePath, cancellationToken);
        var images = await ReadImagesAsync(options.SourceDatabasePath, cancellationToken);
        var roots = await ReadRootsAsync(options.SourceDatabasePath, cancellationToken);

        var missingImages = images.Count(image => !File.Exists(image.AbsolutePath));
        if (missingImages > 0)
        {
            warnings.Add($"{missingImages} CardOps image path(s) are currently missing.");
        }

        if (!options.Apply)
        {
            return await BuildDryRunReportAsync(options, cards, images, roots, warnings, cancellationToken);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(options.TargetDatabasePath)!);
        var backupPath = File.Exists(options.TargetDatabasePath) ? BackupDatabase(options.TargetDatabasePath) : string.Empty;

        await using var db = CreateTargetDbContext(options.TargetDatabasePath);
        await db.Database.MigrateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var tagsByName = await db.Tags.ToDictionaryAsync(tag => tag.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var cardsInserted = 0;
        var cardsUpdated = 0;
        var imagesInserted = 0;
        var imagesUpdated = 0;
        var linksInserted = 0;
        var rootsInserted = 0;
        var tagsInserted = 0;
        var customFieldsInserted = 0;
        var cardIds = cards.Select(card => card.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var photoIdsByCardOpsImageId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            var id = CardOpsId("root", root.Id);
            var existing = await db.SourceRoots.FindAsync([id], cancellationToken) ??
                           await db.SourceRoots.FirstOrDefaultAsync(sourceRoot => sourceRoot.Path == root.Path, cancellationToken);
            if (existing is null)
            {
                db.SourceRoots.Add(new SourceRoot
                {
                    Id = id,
                    Path = root.Path,
                    Enabled = root.RevokedAt is null,
                    Recursive = root.Recursive,
                    ExcludePatterns = string.Join(';', root.ExcludePatterns),
                    CreatedUtc = root.CreatedAt
                });
                rootsInserted++;
            }
            else
            {
                existing.Enabled = root.RevokedAt is null;
                existing.Recursive = root.Recursive;
                existing.ExcludePatterns = string.Join(';', root.ExcludePatterns);
            }
        }

        foreach (var card in cards)
        {
            var item = MapCard(card);
            var existing = await db.InventoryItems.FindAsync([item.Id], cancellationToken);
            if (existing is null)
            {
                db.InventoryItems.Add(item);
                cardsInserted++;
            }
            else
            {
                CopyItem(item, existing);
                cardsUpdated++;
            }

            foreach (var tagName in card.Tags)
            {
                var tag = await GetOrCreateTagAsync(db, tagsByName, tagName, cancellationToken);
                if (!await db.ItemTags.AnyAsync(link => link.InventoryItemId == item.Id && link.TagId == tag.Id, cancellationToken))
                {
                    db.ItemTags.Add(new ItemTag { InventoryItemId = item.Id, TagId = tag.Id });
                    if (tag.CreatedUtc > DateTimeOffset.UtcNow.AddMinutes(-1))
                    {
                        tagsInserted++;
                    }
                }
            }

            customFieldsInserted += await UpsertCustomFieldsAsync(db, item.Id, BuildCustomFields(card), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var image in images)
        {
            var photo = MapImage(image);
            var existingById = await db.Photos.FindAsync([photo.Id], cancellationToken);
            var existingByPath = await db.Photos.FirstOrDefaultAsync(existingPhoto => existingPhoto.FullPath == photo.FullPath, cancellationToken);
            var existing = existingByPath ?? existingById;
            if (existing is null)
            {
                db.Photos.Add(photo);
                imagesInserted++;
                photoIdsByCardOpsImageId[image.Id] = photo.Id;
            }
            else
            {
                CopyPhoto(photo, existing);
                imagesUpdated++;
                photoIdsByCardOpsImageId[image.Id] = existing.Id;
            }

            if (!string.IsNullOrWhiteSpace(image.CardInstanceId))
            {
                var itemId = CardOpsId("card", image.CardInstanceId);
                var photoId = photoIdsByCardOpsImageId[image.Id];
                if (cardIds.Contains(image.CardInstanceId) &&
                    !await db.PhotoItemLinks.AnyAsync(link => link.PhotoId == photoId && link.InventoryItemId == itemId, cancellationToken))
                {
                    db.PhotoItemLinks.Add(new PhotoItemLink
                    {
                        PhotoId = photoId,
                        InventoryItemId = itemId,
                        SortOrder = 1,
                        IsPrimary = true,
                        ViewType = ParseCardOpsViewType(image.FrontBackAssignment),
                        ImageRoleCode = ParseCardOpsViewType(image.FrontBackAssignment).ToString()
                    });
                    linksInserted++;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CardOpsImportReport(
            Applied: true,
            CardsRead: cards.Count,
            CardsInserted: cardsInserted,
            CardsUpdated: cardsUpdated,
            ImagesRead: images.Count,
            ImagesInserted: imagesInserted,
            ImagesUpdated: imagesUpdated,
            LinksInserted: linksInserted,
            SourceRootsRead: roots.Count,
            SourceRootsInserted: rootsInserted,
            TagsInserted: tagsInserted,
            CustomFieldsInserted: customFieldsInserted,
            BackupPath: backupPath,
            Warnings: warnings);
    }

    private static async Task<IReadOnlyList<CardOpsCard>> ReadCardsAsync(string databasePath, CancellationToken cancellationToken)
    {
        var result = new List<CardOpsCard>();
        await using var connection = new SqliteConnection(BuildConnectionString(databasePath, readOnly: true));
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, internal_sku, sport, player, team, manufacturer, brand, set_name, set_year, card_number,
                   subset, variation, parallel, rookie, autograph, relic, serial_number_current, serial_number_total,
                   raw_or_graded, grading_company, grade, quantity, condition_notes, acquisition_cost, estimated_value,
                   verified_sale_low, verified_sale_high, storage_location, current_lot_assignment, current_ebay_listing,
                   processing_status, confidence, tags, created_at, updated_at
            from card_instances
            order by created_at, id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CardOpsCard(
                Id: reader.GetString(0),
                InternalSku: GetText(reader, 1),
                Sport: GetText(reader, 2),
                Player: GetText(reader, 3),
                Team: GetText(reader, 4),
                Manufacturer: GetText(reader, 5),
                Brand: GetText(reader, 6),
                SetName: GetText(reader, 7),
                SetYear: GetInt(reader, 8),
                CardNumber: GetText(reader, 9),
                Subset: GetText(reader, 10),
                Variation: GetText(reader, 11),
                Parallel: GetText(reader, 12),
                Rookie: GetBool(reader, 13),
                Autograph: GetBool(reader, 14),
                Relic: GetBool(reader, 15),
                SerialNumberCurrent: GetInt(reader, 16),
                SerialNumberTotal: GetInt(reader, 17),
                RawOrGraded: GetText(reader, 18),
                GradingCompany: GetText(reader, 19),
                Grade: GetText(reader, 20),
                Quantity: GetInt(reader, 21) ?? 1,
                ConditionNotes: GetText(reader, 22),
                AcquisitionCost: GetDecimal(reader, 23),
                EstimatedValue: GetDecimal(reader, 24),
                VerifiedSaleLow: GetDecimal(reader, 25),
                VerifiedSaleHigh: GetDecimal(reader, 26),
                StorageLocation: GetText(reader, 27),
                CurrentLotAssignment: GetText(reader, 28),
                CurrentEbayListing: GetText(reader, 29),
                ProcessingStatus: GetText(reader, 30),
                Confidence: GetDouble(reader, 31),
                Tags: ReadJsonStringArray(GetText(reader, 32)),
                CreatedAt: GetDate(reader, 33),
                UpdatedAt: GetDate(reader, 34)));
        }

        return result;
    }

    private static async Task<IReadOnlyList<CardOpsImage>> ReadImagesAsync(string databasePath, CancellationToken cancellationToken)
    {
        var result = new List<CardOpsImage>();
        await using var connection = new SqliteConnection(BuildConnectionString(databasePath, readOnly: true));
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, directory_id, absolute_path, relative_path, file_name, extension, file_size, created_time,
                   modified_time, sha256, perceptual_hash, width, height, thumbnail_path, imported_at,
                   processing_status, duplicate_status, front_back_assignment, original_location, card_instance_id, error_message
            from image_assets
            order by imported_at, id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CardOpsImage(
                Id: reader.GetString(0),
                DirectoryId: reader.GetString(1),
                AbsolutePath: reader.GetString(2),
                RelativePath: reader.GetString(3),
                FileName: reader.GetString(4),
                Extension: reader.GetString(5),
                FileSize: GetLong(reader, 6),
                CreatedTime: GetDate(reader, 7),
                ModifiedTime: GetDate(reader, 8),
                Sha256: GetText(reader, 9),
                PerceptualHash: GetText(reader, 10),
                Width: GetInt(reader, 11) ?? 0,
                Height: GetInt(reader, 12) ?? 0,
                ThumbnailPath: GetText(reader, 13),
                ImportedAt: GetDate(reader, 14),
                ProcessingStatus: GetText(reader, 15),
                DuplicateStatus: GetText(reader, 16),
                FrontBackAssignment: GetText(reader, 17),
                OriginalLocation: GetText(reader, 18),
                CardInstanceId: GetText(reader, 19),
                ErrorMessage: GetText(reader, 20)));
        }

        return result;
    }

    private static async Task<IReadOnlyList<CardOpsRoot>> ReadRootsAsync(string databasePath, CancellationToken cancellationToken)
    {
        var result = new List<CardOpsRoot>();
        await using var connection = new SqliteConnection(BuildConnectionString(databasePath, readOnly: true));
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select id, path, label, recursive, exclude_patterns, created_at, revoked_at from directory_roots order by created_at, id";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CardOpsRoot(
                Id: reader.GetString(0),
                Path: reader.GetString(1),
                Label: GetText(reader, 2),
                Recursive: GetBool(reader, 3),
                ExcludePatterns: ReadJsonStringArray(GetText(reader, 4)),
                CreatedAt: GetDate(reader, 5),
                RevokedAt: reader.IsDBNull(6) ? null : GetDate(reader, 6)));
        }

        return result;
    }

    private static async Task<CardOpsImportReport> BuildDryRunReportAsync(
        CardOpsImportOptions options,
        IReadOnlyList<CardOpsCard> cards,
        IReadOnlyList<CardOpsImage> images,
        IReadOnlyList<CardOpsRoot> roots,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var cardsInserted = cards.Count;
        var cardsUpdated = 0;
        var imagesInserted = images.Count;
        var imagesUpdated = 0;
        var rootsInserted = roots.Count;
        var linksInserted = images.Count(image => !string.IsNullOrWhiteSpace(image.CardInstanceId));
        var tagsInserted = CountDistinctTags(cards);
        var customFieldsInserted = CountCustomFields(cards);

        if (File.Exists(options.TargetDatabasePath))
        {
            try
            {
                await using var db = CreateTargetDbContext(options.TargetDatabasePath, readOnly: true);
                var cardIds = cards.Select(card => CardOpsId("card", card.Id)).ToList();
                var existingCardIds = (await db.InventoryItems
                    .Where(item => cardIds.Contains(item.Id))
                    .Select(item => item.Id)
                    .ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                cardsUpdated = cards.Count(card => existingCardIds.Contains(CardOpsId("card", card.Id)));
                cardsInserted = cards.Count - cardsUpdated;

                var imageIds = images.Select(image => CardOpsId("image", image.Id)).ToList();
                var imagePaths = images.Select(image => image.AbsolutePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existingPhotos = await db.Photos
                    .Where(photo => imageIds.Contains(photo.Id) || imagePaths.Contains(photo.FullPath))
                    .Select(photo => new { photo.Id, photo.FullPath })
                    .ToListAsync(cancellationToken);
                var existingPhotoIds = existingPhotos.Select(photo => photo.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingPhotoPaths = existingPhotos.Select(photo => photo.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);

                imagesUpdated = images.Count(image =>
                    existingPhotoIds.Contains(CardOpsId("image", image.Id)) ||
                    existingPhotoPaths.Contains(image.AbsolutePath));
                imagesInserted = images.Count - imagesUpdated;

                var rootIds = roots.Select(root => CardOpsId("root", root.Id)).ToList();
                var rootPaths = roots.Select(root => root.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existingRoots = await db.SourceRoots
                    .Where(root => rootIds.Contains(root.Id) || rootPaths.Contains(root.Path))
                    .Select(root => new { root.Id, root.Path })
                    .ToListAsync(cancellationToken);
                var existingRootIds = existingRoots.Select(root => root.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingRootPaths = existingRoots.Select(root => root.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

                rootsInserted = roots.Count(root =>
                    !existingRootIds.Contains(CardOpsId("root", root.Id)) &&
                    !existingRootPaths.Contains(root.Path));

                var distinctTags = cards
                    .SelectMany(card => card.Tags)
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => tag.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var existingTags = (await db.Tags
                    .Where(tag => distinctTags.Contains(tag.Name))
                    .Select(tag => tag.Name)
                    .ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                tagsInserted = distinctTags.Count(tag => !existingTags.Contains(tag));

                var existingFields = (await db.CustomFieldValues
                    .Where(field => cardIds.Contains(field.InventoryItemId))
                    .Select(field => new { field.InventoryItemId, field.FieldName })
                    .ToListAsync(cancellationToken))
                    .Select(field => CompositeKey(field.InventoryItemId, field.FieldName))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                customFieldsInserted = cards.Sum(card =>
                {
                    var itemId = CardOpsId("card", card.Id);
                    return BuildCustomFields(card).Count(field =>
                        !string.IsNullOrWhiteSpace(field.Value) &&
                        !existingFields.Contains(CompositeKey(itemId, field.Key)));
                });

                var sourceCardIds = cards.Select(card => card.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var photosById = existingPhotos
                    .GroupBy(photo => photo.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
                var photosByPath = existingPhotos
                    .GroupBy(photo => photo.FullPath, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
                var candidateLinks = images
                    .Where(image => !string.IsNullOrWhiteSpace(image.CardInstanceId) && sourceCardIds.Contains(image.CardInstanceId))
                    .Select(image =>
                    {
                        var generatedPhotoId = CardOpsId("image", image.Id);
                        var photoId = photosByPath.GetValueOrDefault(image.AbsolutePath) ??
                                      photosById.GetValueOrDefault(generatedPhotoId) ??
                                      generatedPhotoId;
                        return new
                        {
                            PhotoId = photoId,
                            ItemId = CardOpsId("card", image.CardInstanceId)
                        };
                    })
                    .ToList();
                var candidatePhotoIds = candidateLinks.Select(link => link.PhotoId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var candidateItemIds = candidateLinks.Select(link => link.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existingLinks = (await db.PhotoItemLinks
                    .Where(link => candidatePhotoIds.Contains(link.PhotoId) && candidateItemIds.Contains(link.InventoryItemId))
                    .Select(link => new { link.PhotoId, link.InventoryItemId })
                    .ToListAsync(cancellationToken))
                    .Select(link => CompositeKey(link.PhotoId, link.InventoryItemId))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                linksInserted = candidateLinks.Count(link => !existingLinks.Contains(CompositeKey(link.PhotoId, link.ItemId)));
            }
            catch (Exception ex) when (ex is SqliteException or InvalidOperationException)
            {
                warnings.Add($"Dry-run target inspection failed, falling back to source-only counts: {ex.Message}");
            }
        }

        return new CardOpsImportReport(
            Applied: false,
            CardsRead: cards.Count,
            CardsInserted: cardsInserted,
            CardsUpdated: cardsUpdated,
            ImagesRead: images.Count,
            ImagesInserted: imagesInserted,
            ImagesUpdated: imagesUpdated,
            LinksInserted: linksInserted,
            SourceRootsRead: roots.Count,
            SourceRootsInserted: rootsInserted,
            TagsInserted: tagsInserted,
            CustomFieldsInserted: customFieldsInserted,
            BackupPath: string.Empty,
            Warnings: warnings);
    }

    private static InventoryItem MapCard(CardOpsCard card)
    {
        var nameParts = new[]
        {
            card.SetYear?.ToString(CultureInfo.InvariantCulture),
            FirstNonEmpty(card.Brand, card.Manufacturer),
            card.SetName,
            card.Player,
            string.IsNullOrWhiteSpace(card.CardNumber) ? string.Empty : "#" + card.CardNumber
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        var serial = card.SerialNumberCurrent.HasValue && card.SerialNumberTotal.HasValue
            ? $"{card.SerialNumberCurrent}/{card.SerialNumberTotal}"
            : card.SerialNumberCurrent?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        return new InventoryItem
        {
            Id = CardOpsId("card", card.Id),
            Name = FirstNonEmpty(string.Join(' ', nameParts), card.InternalSku, "Imported CardOps Card"),
            Category = "Trading Cards",
            SportOrGame = card.Sport,
            PlayerOrTitle = card.Player,
            Year = card.SetYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Brand = FirstNonEmpty(card.Brand, card.Manufacturer),
            SetName = card.SetName,
            CardNumber = card.CardNumber,
            SerialNumber = serial,
            Rookie = card.Rookie,
            Autograph = card.Autograph,
            Relic = card.Relic,
            Team = card.Team,
            Manufacturer = card.Manufacturer,
            Condition = card.ConditionNotes,
            GradingCompany = card.GradingCompany,
            Grade = card.Grade,
            ListingStatus = string.IsNullOrWhiteSpace(card.CurrentEbayListing) ? ListingStatus.NotListed : ListingStatus.CurrentlyListed,
            ListingPlatform = string.IsNullOrWhiteSpace(card.CurrentEbayListing) ? string.Empty : "eBay",
            ListingId = card.CurrentEbayListing,
            CreatedUtc = card.CreatedAt,
            ModifiedUtc = card.UpdatedAt,
            Notes = BuildCardNotes(card)
        };
    }

    private static Photo MapImage(CardOpsImage image) => new()
    {
        Id = CardOpsId("image", image.Id),
        FullPath = image.AbsolutePath,
        OriginalPath = image.OriginalLocation,
        FileName = image.FileName,
        Extension = image.Extension.ToLowerInvariant(),
        FileSize = image.FileSize,
        Width = image.Width,
        Height = image.Height,
        CreatedUtc = image.CreatedTime,
        ModifiedUtc = image.ModifiedTime,
        ImportedUtc = image.ImportedAt,
        Sha256 = image.Sha256,
        IsMissing = !File.Exists(image.AbsolutePath),
        IsDuplicate = image.DuplicateStatus.Contains("duplicate", StringComparison.OrdinalIgnoreCase),
        ThumbnailPath = image.ThumbnailPath,
        ViewType = ParseCardOpsViewType(image.FrontBackAssignment),
        Notes = BuildImageNotes(image)
    };

    private static IReadOnlyDictionary<string, string> BuildCustomFields(CardOpsCard card) => new Dictionary<string, string>
    {
        ["CardOps.InternalSku"] = card.InternalSku,
        ["CardOps.Subset"] = card.Subset,
        ["CardOps.Variation"] = card.Variation,
        ["CardOps.Parallel"] = card.Parallel,
        ["CardOps.RawOrGraded"] = card.RawOrGraded,
        ["CardOps.StorageLocation"] = card.StorageLocation,
        ["CardOps.CurrentLotAssignment"] = card.CurrentLotAssignment,
        ["CardOps.ProcessingStatus"] = card.ProcessingStatus,
        ["CardOps.Confidence"] = card.Confidence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        ["CardOps.AcquisitionCost"] = card.AcquisitionCost?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        ["CardOps.EstimatedValue"] = card.EstimatedValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        ["CardOps.VerifiedSaleLow"] = card.VerifiedSaleLow?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        ["CardOps.VerifiedSaleHigh"] = card.VerifiedSaleHigh?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static async Task<int> UpsertCustomFieldsAsync(
        InventoryDbContext db,
        string itemId,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        foreach (var field in fields.Where(field => !string.IsNullOrWhiteSpace(field.Value)))
        {
            var existing = await db.CustomFieldValues.FirstOrDefaultAsync(
                value => value.InventoryItemId == itemId && value.FieldName == field.Key,
                cancellationToken);
            if (existing is null)
            {
                db.CustomFieldValues.Add(new CustomFieldValue
                {
                    InventoryItemId = itemId,
                    FieldName = field.Key,
                    FieldValue = field.Value
                });
                inserted++;
            }
            else
            {
                existing.FieldValue = field.Value;
            }
        }

        return inserted;
    }

    private static async Task<Tag> GetOrCreateTagAsync(
        InventoryDbContext db,
        Dictionary<string, Tag> tagsByName,
        string tagName,
        CancellationToken cancellationToken)
    {
        var normalized = tagName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Tag name cannot be blank.");
        }

        if (tagsByName.TryGetValue(normalized, out var existing))
        {
            return existing;
        }

        var tag = new Tag { Name = normalized };
        db.Tags.Add(tag);
        tagsByName[normalized] = tag;
        await db.SaveChangesAsync(cancellationToken);
        return tag;
    }

    private static void CopyItem(InventoryItem source, InventoryItem target)
    {
        target.Name = source.Name;
        target.Category = source.Category;
        target.SportOrGame = source.SportOrGame;
        target.PlayerOrTitle = source.PlayerOrTitle;
        target.Year = source.Year;
        target.Brand = source.Brand;
        target.SetName = source.SetName;
        target.CardNumber = source.CardNumber;
        target.SerialNumber = source.SerialNumber;
        target.Rookie = source.Rookie;
        target.Autograph = source.Autograph;
        target.Relic = source.Relic;
        target.Team = source.Team;
        target.Manufacturer = source.Manufacturer;
        target.Condition = source.Condition;
        target.GradingCompany = source.GradingCompany;
        target.Grade = source.Grade;
        target.ListingStatus = source.ListingStatus;
        target.ListingPlatform = source.ListingPlatform;
        target.ListingId = source.ListingId;
        target.Notes = source.Notes;
        target.ModifiedUtc = DateTimeOffset.UtcNow;
    }

    private static void CopyPhoto(Photo source, Photo target)
    {
        target.FullPath = source.FullPath;
        target.OriginalPath = source.OriginalPath;
        target.FileName = source.FileName;
        target.Extension = source.Extension;
        target.FileSize = source.FileSize;
        target.Width = source.Width;
        target.Height = source.Height;
        target.ModifiedUtc = source.ModifiedUtc;
        target.Sha256 = source.Sha256;
        target.IsMissing = source.IsMissing;
        target.IsDuplicate = source.IsDuplicate;
        target.ThumbnailPath = source.ThumbnailPath;
        target.ViewType = source.ViewType;
        target.Notes = source.Notes;
    }

    private static InventoryDbContext CreateTargetDbContext(string databasePath, bool readOnly = false)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(BuildConnectionString(databasePath, readOnly))
            .Options;
        return new InventoryDbContext(options);
    }

    private static string BuildConnectionString(string databasePath, bool readOnly)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = databasePath };
        if (readOnly)
        {
            builder.Mode = SqliteOpenMode.ReadOnly;
        }

        return builder.ToString();
    }

    private static string BackupDatabase(string databasePath)
    {
        var backupDir = Path.Combine(Path.GetDirectoryName(databasePath)!, "backups");
        Directory.CreateDirectory(backupDir);
        var backupPath = Path.Combine(backupDir, $"cardops-import-target.{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}.sqlite");
        File.Copy(databasePath, backupPath, overwrite: false);
        return backupPath;
    }

    private static bool IsLiveInventoryPath(string path) =>
        Path.GetFullPath(path).StartsWith(@"D:\INVENTORY_PHOTO_OPS", StringComparison.OrdinalIgnoreCase);

    private static string CardOpsId(string kind, string id) => $"cardops-{kind}-{id}";

    private static string CompositeKey(string left, string right) => $"{left}|{right}";

    private static int CountDistinctTags(IEnumerable<CardOpsCard> cards) =>
        cards.SelectMany(card => card.Tags).Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).Count();

    private static int CountCustomFields(IEnumerable<CardOpsCard> cards) =>
        cards.Sum(card => BuildCustomFields(card).Count(field => !string.IsNullOrWhiteSpace(field.Value)));

    private static string BuildCardNotes(CardOpsCard card) => string.Join(
        Environment.NewLine,
        new[]
        {
            "Imported from CardOps.",
            string.IsNullOrWhiteSpace(card.ProcessingStatus) ? string.Empty : $"Processing status: {card.ProcessingStatus}",
            string.IsNullOrWhiteSpace(card.CurrentLotAssignment) ? string.Empty : $"Lot assignment: {card.CurrentLotAssignment}",
            string.IsNullOrWhiteSpace(card.ConditionNotes) ? string.Empty : $"Condition notes: {card.ConditionNotes}"
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string BuildImageNotes(CardOpsImage image) => string.Join(
        Environment.NewLine,
        new[]
        {
            "Imported from CardOps image asset.",
            string.IsNullOrWhiteSpace(image.ProcessingStatus) ? string.Empty : $"Processing status: {image.ProcessingStatus}",
            string.IsNullOrWhiteSpace(image.DuplicateStatus) ? string.Empty : $"Duplicate status: {image.DuplicateStatus}",
            string.IsNullOrWhiteSpace(image.ErrorMessage) ? string.Empty : $"CardOps error: {image.ErrorMessage}",
            string.IsNullOrWhiteSpace(image.PerceptualHash) ? string.Empty : $"CardOps perceptual hash: {image.PerceptualHash}",
            string.IsNullOrWhiteSpace(image.RelativePath) ? string.Empty : $"CardOps relative path: {image.RelativePath}",
            string.IsNullOrWhiteSpace(image.DirectoryId) ? string.Empty : $"CardOps directory id: {image.DirectoryId}"
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static PhotoViewType ParseCardOpsViewType(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "front" => PhotoViewType.Front,
            "back" => PhotoViewType.Back,
            _ => PhotoViewType.Unknown
        };

    private static IReadOnlyList<string> ReadJsonStringArray(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string GetText(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? string.Empty;

    private static int? GetInt(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static long GetLong(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static bool GetBool(SqliteDataReader reader, int ordinal) =>
        !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static decimal? GetDecimal(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static double? GetDouble(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static DateTimeOffset GetDate(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return DateTimeOffset.UtcNow;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTime date => new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)),
            string text when DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) => parsed.ToUniversalTime(),
            _ => DateTimeOffset.UtcNow
        };
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private sealed record CardOpsCard(
        string Id,
        string InternalSku,
        string Sport,
        string Player,
        string Team,
        string Manufacturer,
        string Brand,
        string SetName,
        int? SetYear,
        string CardNumber,
        string Subset,
        string Variation,
        string Parallel,
        bool Rookie,
        bool Autograph,
        bool Relic,
        int? SerialNumberCurrent,
        int? SerialNumberTotal,
        string RawOrGraded,
        string GradingCompany,
        string Grade,
        int Quantity,
        string ConditionNotes,
        decimal? AcquisitionCost,
        decimal? EstimatedValue,
        decimal? VerifiedSaleLow,
        decimal? VerifiedSaleHigh,
        string StorageLocation,
        string CurrentLotAssignment,
        string CurrentEbayListing,
        string ProcessingStatus,
        double? Confidence,
        IReadOnlyList<string> Tags,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record CardOpsImage(
        string Id,
        string DirectoryId,
        string AbsolutePath,
        string RelativePath,
        string FileName,
        string Extension,
        long FileSize,
        DateTimeOffset CreatedTime,
        DateTimeOffset ModifiedTime,
        string Sha256,
        string PerceptualHash,
        int Width,
        int Height,
        string ThumbnailPath,
        DateTimeOffset ImportedAt,
        string ProcessingStatus,
        string DuplicateStatus,
        string FrontBackAssignment,
        string OriginalLocation,
        string CardInstanceId,
        string ErrorMessage);

    private sealed record CardOpsRoot(
        string Id,
        string Path,
        string Label,
        bool Recursive,
        IReadOnlyList<string> ExcludePatterns,
        DateTimeOffset CreatedAt,
        DateTimeOffset? RevokedAt);
}
