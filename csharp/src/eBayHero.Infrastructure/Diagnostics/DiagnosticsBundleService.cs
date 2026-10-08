using System.Globalization;
using System.Reflection;
using System.Text.Json;
using eBayHero.Core.Configuration;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using eBayHero.Infrastructure.Data;
using eBayHero.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBayHero.Infrastructure.Diagnostics;

public sealed class DiagnosticsBundleService(
    InventoryOptions options,
    IDbContextFactory<InventoryDbContext> dbContextFactory,
    DatabaseMaintenanceService databaseMaintenance,
    RedactingBundleWriter writer,
    ILogger<DiagnosticsBundleService> logger) : IDiagnosticsBundleService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<DiagnosticsBundleResult> CollectAsync(DiagnosticsBundleRequest request, CancellationToken cancellationToken)
    {
        var bundleRoot = Path.Combine(request.OperationsRoot, "DiagnosticBundles");
        var bundleDirectory = Path.Combine(bundleRoot, $"DiagnosticBundle-{DateTimeOffset.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(bundleDirectory);

        var warnings = new List<string>();
        var sectionNames = new List<string>();
        foreach (var group in CreateSectionGroups(request))
        {
            try
            {
                foreach (var section in await group.Produce(cancellationToken))
                {
                    await writer.WriteSectionAsync(bundleDirectory, section.RelativeName, section.Content, cancellationToken);
                    sectionNames.Add(section.RelativeName);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Diagnostics section {SectionName} failed", group.Name);
                warnings.Add($"{group.Name}: {ex.Message}");
                var errorName = $"errors/{group.Name}.txt";
                await writer.WriteSectionAsync(bundleDirectory, errorName, $"Collection failed: {ex}", cancellationToken);
                sectionNames.Add(errorName);
            }
        }

        var summary = $"""
            # Diagnostic Bundle

            - Created: {DateTimeOffset.Now:O}
            - Operations root: {request.OperationsRoot}
            - Sections: {sectionNames.Count}

            This bundle intentionally excludes secrets, raw inventory databases, raw images, buyer data, and token files.
            """;
        await writer.WriteSectionAsync(bundleDirectory, "SUMMARY.md", summary, cancellationToken);
        await writer.WriteSectionAsync(
            bundleDirectory,
            "manifest.json",
            Serialize(new { createdUtc = DateTimeOffset.UtcNow, sections = sectionNames, warnings }),
            cancellationToken);

        var zipPath = writer.CreateZip(bundleDirectory);
        return new DiagnosticsBundleResult(bundleDirectory, zipPath, sectionNames.Count, warnings);
    }

    private IReadOnlyList<SectionGroup> CreateSectionGroups(DiagnosticsBundleRequest request) =>
    [
        new("application.json", async cancellationToken =>
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
            var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
            return
            [
                new Section("application.json", Serialize(new
                {
                    product = "eBay Hero",
                    appVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
                    schemaVersion = applied.LastOrDefault() ?? "none",
                    pendingMigrations = pending.Count()
                }))
            ];
        }),
        new("environment.json", _ => Task.FromResult<IReadOnlyList<Section>>(
        [
            new Section("environment.json", Serialize(new
            {
                os = Environment.OSVersion.VersionString,
                is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                machine = Environment.MachineName,
                user = Environment.UserName,
                runtimeVersion = Environment.Version.ToString(),
                currentCulture = CultureInfo.CurrentCulture.Name
            }))
        ])),
        new("config-redacted.json", _ => Task.FromResult<IReadOnlyList<Section>>(
        [
            new Section("config-redacted.json", Serialize(new InventoryOptions
            {
                OperationsRoot = request.OperationsRoot,
                DefaultSourceRoot = options.DefaultSourceRoot,
                TesseractPath = options.TesseractPath,
                OcrLanguage = options.OcrLanguage,
                OcrMinimumConfidence = options.OcrMinimumConfidence,
                OcrUpscaleFactor = options.OcrUpscaleFactor,
                ThumbnailSize = options.ThumbnailSize,
                AutoGroupSeconds = options.AutoGroupSeconds,
                MaxAutomaticGroupSize = options.MaxAutomaticGroupSize,
                DefaultListingStatus = options.DefaultListingStatus,
                DevelopmentSafeMode = options.DevelopmentSafeMode
            }))
        ])),
        new("database.json", async cancellationToken =>
        {
            var integrityPassed = await databaseMaintenance.CheckIntegrityAsync(cancellationToken);
            var databasePath = options.DatabasePath;
            return
            [
                new Section("database.json", Serialize(new
                {
                    databasePath,
                    exists = File.Exists(databasePath),
                    sizeBytes = File.Exists(databasePath) ? new FileInfo(databasePath).Length : 0,
                    integrityCheckPassed = integrityPassed
                }))
            ];
        }),
        new("recent-actions.json", async cancellationToken =>
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var events = await db.AuditEvents
                .OrderByDescending(e => e.CreatedUtc)
                .Take(request.MaxAuditEvents)
                .Select(e => new { e.CreatedUtc, e.Severity, e.Operation, e.JobId, e.PhotoId, e.ItemId, e.Message, e.Exception })
                .ToListAsync(cancellationToken);
            return [new Section("recent-actions.json", Serialize(events))];
        }),
        new("failed-jobs.json", async cancellationToken =>
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var jobs = await db.Jobs
                .Where(j => j.Status == JobStatus.Failed)
                .OrderByDescending(j => j.CreatedUtc)
                .Take(request.MaxFailedJobs)
                .Select(j => new { j.Id, j.Kind, j.Status, j.CreatedUtc, j.StartedUtc, j.EndedUtc, j.CurrentFile, j.ErrorDetails, j.PayloadJson })
                .ToListAsync(cancellationToken);
            var jobIds = jobs.Select(j => j.Id).ToList();
            var attempts = await db.JobAttempts
                .Where(a => jobIds.Contains(a.JobId))
                .Select(a => new { a.JobId, a.AttemptNumber, a.StartedUtc, a.EndedUtc, a.ResultJson, a.ErrorDetails })
                .ToListAsync(cancellationToken);
            return [new Section("failed-jobs.json", Serialize(new { jobs, attempts }))];
        }),
        new("logs", async cancellationToken =>
        {
            var logRoot = Path.Combine(request.OperationsRoot, "logs");
            if (!Directory.Exists(logRoot))
            {
                return [new Section("logs/README.txt", $"No log directory was found at {logRoot}.")];
            }

            var sections = new List<Section>();
            var logFiles = new DirectoryInfo(logRoot)
                .EnumerateFiles("eBayHero_*.log")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(request.MaxLogFiles);
            foreach (var file in logFiles)
            {
                if (file.Length > request.MaxLogFileBytes)
                {
                    sections.Add(new Section(
                        $"logs/{file.Name}.skipped.txt",
                        $"Skipped {file.Name}: {file.Length} bytes exceeds the {request.MaxLogFileBytes} byte limit."));
                    continue;
                }

                using var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                var lines = content.Split('\n');
                if (lines.Length > request.MaxLogTailLines)
                {
                    content = string.Join('\n', lines[^request.MaxLogTailLines..]);
                }

                sections.Add(new Section($"logs/{file.Name}.redacted.txt", content));
            }

            return sections;
        })
    ];

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed record Section(string RelativeName, string Content);

    private sealed record SectionGroup(string Name, Func<CancellationToken, Task<IReadOnlyList<Section>>> Produce);
}
