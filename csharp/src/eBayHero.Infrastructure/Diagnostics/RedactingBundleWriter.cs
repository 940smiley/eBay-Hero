using System.IO.Compression;
using System.Text;
using eBayHero.Core.Services;

namespace eBayHero.Infrastructure.Diagnostics;

public sealed class RedactingBundleWriter(ISecretRedactor redactor)
{
    public async Task WriteSectionAsync(string bundleDirectory, string relativeName, string content, CancellationToken cancellationToken)
    {
        var path = GetSafeSectionPath(bundleDirectory, relativeName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, redactor.Redact(content), Encoding.UTF8, cancellationToken);
    }

    public string CreateZip(string bundleDirectory)
    {
        var zipPath = bundleDirectory + ".zip";
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(bundleDirectory, zipPath);
        return zipPath;
    }

    public static string GetSafeSectionPath(string bundleDirectory, string relativeName)
    {
        if (string.IsNullOrWhiteSpace(relativeName) || Path.IsPathRooted(relativeName))
        {
            throw new ArgumentException("Section name must be a non-empty relative path.", nameof(relativeName));
        }

        var root = Path.GetFullPath(bundleDirectory);
        var full = Path.GetFullPath(Path.Combine(root, relativeName));
        if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Section name must stay under the bundle directory: {relativeName}", nameof(relativeName));
        }

        return full;
    }
}
