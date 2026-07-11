using System.Runtime.CompilerServices;
using InventoryPhotoOps.Core.Configuration;
using InventoryPhotoOps.Core.Models;
using InventoryPhotoOps.Core.Services;
using Microsoft.Extensions.Logging;

namespace InventoryPhotoOps.FileSystem;

public sealed class FileScanner(IPathService pathService, ILogger<FileScanner> logger) : IFileScanner
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff", ".gif", ".heic", ".avif"
    };

    public async IAsyncEnumerable<ScanPhotoResult> ScanAsync(
        SourceRoot root,
        InventoryOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rootPath = pathService.NormalizePath(root.Path);
        if (!root.Enabled || !Directory.Exists(rootPath))
        {
            yield break;
        }

        var excludedRoots = GetExcludedRoots(options).Select(pathService.NormalizePath).ToList();
        var searchOption = root.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(rootPath, "*", searchOption);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to enumerate source root {Root}", rootPath);
            yield break;
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalized = pathService.NormalizePath(file);
            if (excludedRoots.Any(excluded => pathService.IsSamePath(normalized, excluded) || pathService.IsChildOf(normalized, excluded)))
            {
                continue;
            }

            if (!SupportedExtensions.Contains(Path.GetExtension(normalized)))
            {
                continue;
            }

            FileInfo info;
            try
            {
                info = new FileInfo(normalized);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Skipping unreadable file {Path}", normalized);
                continue;
            }

            yield return new ScanPhotoResult(
                FullPath: info.FullName,
                NormalizedPath: normalized,
                FileSize: info.Length,
                ModifiedUtc: info.LastWriteTimeUtc,
                Extension: info.Extension.ToLowerInvariant());

            await Task.Yield();
        }
    }

    private static IEnumerable<string> GetExcludedRoots(InventoryOptions options)
    {
        yield return Path.Combine(options.OperationsRoot, "ebay-temp");
        yield return Path.Combine(options.OperationsRoot, "db");
        yield return Path.Combine(options.OperationsRoot, "logs");
        yield return Path.Combine(options.OperationsRoot, "app");
        yield return Path.Combine(options.OperationsRoot, "ocr-training", "temp");
    }
}

