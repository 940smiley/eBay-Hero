using eBayHero.Core.Models;
using eBayHero.Core.Services;
using Microsoft.Extensions.Logging;

namespace eBayHero.FileSystem;

public sealed class FileOperationPlanner(
    IPathService pathService,
    IHashService hashService,
    ILogger<FileOperationPlanner> logger) : IFileOperationPlanner
{
    public async Task<FileOperationPlan> PlanImportAsync(
        string sourceRoot,
        string destinationRoot,
        FileOperationKind operationKind,
        bool preserveFolders,
        IReadOnlySet<string> indexedPaths,
        IReadOnlySet<string> knownHashes,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException(sourceRoot);
        }

        var sourceFull = pathService.NormalizePath(sourceRoot);
        var destinationFull = pathService.NormalizePath(destinationRoot);
        var files = Directory.EnumerateFiles(sourceFull, "*", SearchOption.AllDirectories)
            .Where(IsSupportedImage)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<ImportPlanItem>();
        var reservedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var collisions = 0;
        var duplicates = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalized = pathService.NormalizePath(file);
            var hash = string.Empty;
            try
            {
                hash = await hashService.ComputeSha256Async(normalized, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unable to hash {Path}; planning continues without hash", normalized);
            }

            var alreadyIndexed = indexedPaths.Contains(normalized);
            var duplicate = !string.IsNullOrWhiteSpace(hash) && knownHashes.Contains(hash);
            if (duplicate) duplicates++;

            var destination = operationKind == FileOperationKind.IndexInPlace
                ? normalized
                : BuildDestination(sourceFull, destinationFull, normalized, preserveFolders);

            var collisionSafe = GetUniqueDestination(destination, reservedDestinations);
            if (!string.Equals(destination, collisionSafe, StringComparison.OrdinalIgnoreCase))
            {
                collisions++;
            }

            reservedDestinations.Add(collisionSafe);
            results.Add(new ImportPlanItem(
                normalized,
                collisionSafe,
                operationKind,
                alreadyIndexed,
                duplicate,
                hash));
        }

        return new FileOperationPlan(
            results,
            collisions,
            duplicates,
            TouchesLiveInventory(sourceFull) || TouchesLiveInventory(destinationFull));
    }

    private static bool IsSupportedImage(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".tif", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".avif", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDestination(string sourceRoot, string destinationRoot, string sourceFile, bool preserveFolders)
    {
        var relative = preserveFolders
            ? Path.GetRelativePath(sourceRoot, sourceFile)
            : Path.GetFileName(sourceFile);
        return Path.Combine(destinationRoot, relative);
    }

    private static string GetUniqueDestination(string destination, HashSet<string> reservedDestinations)
    {
        var dir = Path.GetDirectoryName(destination)!;
        var name = Path.GetFileNameWithoutExtension(destination);
        var ext = Path.GetExtension(destination);
        var candidate = destination;
        var index = 2;

        while (File.Exists(candidate) || reservedDestinations.Contains(candidate))
        {
            candidate = Path.Combine(dir, $"{name}_{index}{ext}");
            index++;
        }

        return candidate;
    }

    private static bool TouchesLiveInventory(string path) =>
        path.StartsWith(@"F:\Inventory", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(@"D:\INVENTORY_PHOTO_OPS", StringComparison.OrdinalIgnoreCase);
}


