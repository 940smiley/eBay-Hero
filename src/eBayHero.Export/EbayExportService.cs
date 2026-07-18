using System.Text;
using System.Text.Json;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using Microsoft.Extensions.Logging;

namespace eBayHero.Export;

public sealed class EbayExportService(ILogger<EbayExportService> logger) : IEbayExportService
{
    public async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("No inventory items were selected for export.");
        }

        Directory.CreateDirectory(request.ExportRoot);
        var runDirectory = Path.Combine(request.ExportRoot, DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(runDirectory);

        var comparer = new ExportPhotoOrderComparer();
        var manifest = new List<EbayManifestRow>();
        var itemOrder = 1;
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!request.PhotosByItemId.TryGetValue(item.Id, out var photos) || photos.Count == 0)
            {
                logger.LogWarning("Skipping export item {ItemId}; no photos were supplied", item.Id);
                continue;
            }

            var safeItemName = FilenameSanitizer.Sanitize(FirstNonEmpty(item.Name, item.PlayerOrTitle, "Item"), 90, "Item");
            var orderedPhotos = photos.Order(comparer).ToList();
            for (var photoOrder = 1; photoOrder <= orderedPhotos.Count; photoOrder++)
            {
                var photo = orderedPhotos[photoOrder - 1];
                if (!File.Exists(photo.FullPath))
                {
                    logger.LogWarning("Skipping missing export photo {PhotoId}: {Path}", photo.Id, photo.FullPath);
                    continue;
                }

                var extension = string.IsNullOrWhiteSpace(photo.Extension)
                    ? Path.GetExtension(photo.FullPath)
                    : photo.Extension;
                var view = photo.ViewType == PhotoViewType.Unknown ? "Image" : photo.ViewType.ToString();
                var exportName = FilenameSanitizer.Sanitize($"{itemOrder:D3}__{safeItemName}__{photoOrder:D2}__{view}", 150, "image") + extension.ToLowerInvariant();
                exportName = GetUniqueFileName(exportName, usedNames);
                var exportPath = Path.Combine(runDirectory, exportName);

                await using (var source = File.Open(photo.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                await using (var destination = File.Create(exportPath))
                {
                    await source.CopyToAsync(destination, cancellationToken);
                }

                manifest.Add(new EbayManifestRow
                {
                    ItemId = item.Id,
                    GroupName = item.Name,
                    ItemOrder = itemOrder,
                    PhotoId = photo.Id,
                    PhotoOrder = photoOrder,
                    ViewType = photo.ViewType.ToString(),
                    SourcePath = photo.FullPath,
                    ExportPath = exportPath,
                    ListingStatus = item.ListingStatus.ToString(),
                    Category = item.Category,
                    SportGame = item.SportOrGame,
                    PlayerTitle = item.PlayerOrTitle,
                    Year = item.Year,
                    Brand = item.Brand,
                    SetName = item.SetName,
                    CardNumber = item.CardNumber,
                    SerialNumber = item.SerialNumber,
                    Tags = string.Join("; ", item.ItemTags.Select(t => t.Tag?.Name).Where(t => !string.IsNullOrWhiteSpace(t))),
                    OcrText = photo.OcrText,
                    OcrConfidence = photo.OcrConfidence
                });
            }

            itemOrder++;
        }

        var csvPath = Path.Combine(runDirectory, "ebay_export_manifest.csv");
        var jsonPath = Path.Combine(runDirectory, "ebay_export_manifest.json");
        var readmePath = Path.Combine(runDirectory, "README.txt");
        await File.WriteAllTextAsync(csvPath, ToCsv(manifest), Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        await File.WriteAllTextAsync(readmePath, BuildReadme(manifest, request.OfferedStatusChange), cancellationToken);

        return new ExportResult(runDirectory, csvPath, jsonPath, readmePath, request.Items.Count, manifest.Count);
    }

    private static string ToCsv(IReadOnlyList<EbayManifestRow> rows)
    {
        var builder = new StringBuilder();
        var headers = typeof(EbayManifestRow).GetProperties().Select(p => p.Name).ToList();
        builder.AppendLine(string.Join(',', headers));
        foreach (var row in rows)
        {
            var values = headers.Select(header =>
            {
                var value = Convert.ToString(typeof(EbayManifestRow).GetProperty(header)!.GetValue(row)) ?? string.Empty;
                return CsvEscape(value);
            });
            builder.AppendLine(string.Join(',', values));
        }

        return builder.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static string BuildReadme(IReadOnlyList<EbayManifestRow> rows, ListingStatus? offeredStatusChange)
    {
        return $"""
        eBay Hero eBay Export
        Created: {DateTimeOffset.Now:O}
        Items: {rows.Select(r => r.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).Count()}
        Photos: {rows.Count}

        This export contains copies only. Original inventory files were not moved or renamed.
        Images are ordered by item and then by view type: front, back/rear, details, then remaining files.
        Manifest files:
        - ebay_export_manifest.csv
        - ebay_export_manifest.json

        Offered listing status after export: {offeredStatusChange?.ToString() ?? "None"}
        """;
    }

    private static string GetUniqueFileName(string fileName, HashSet<string> usedNames)
    {
        var candidate = fileName;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var index = 2;
        while (!usedNames.Add(candidate))
        {
            candidate = $"{name}_{index}{extension}";
            index++;
        }

        return candidate;
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private sealed class EbayManifestRow
    {
        public string ItemId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int ItemOrder { get; set; }
        public string PhotoId { get; set; } = string.Empty;
        public int PhotoOrder { get; set; }
        public string ViewType { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string ExportPath { get; set; } = string.Empty;
        public string ListingStatus { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SportGame { get; set; } = string.Empty;
        public string PlayerTitle { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string SetName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string OcrText { get; set; } = string.Empty;
        public double OcrConfidence { get; set; }
    }
}


