using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBayHero.Core.Configuration;
using eBayHero.Core.Licensing;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using eBayHero.Infrastructure.Data;
using eBayHero.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBayHero.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly string[] DefaultCategoryOptions =
        InventorySortTaxonomy.CategoryOptions.ToArray();

    private static readonly string[] DefaultSportOrGameOptions =
        InventorySortTaxonomy.GetSubcategoryOptions().ToArray();

    private static readonly string[] DefaultBrandOptions =
    [
        "Topps",
        "Topps Chrome",
        "Topps Heritage",
        "Bowman",
        "Bowman Chrome",
        "Panini",
        "Donruss",
        "Fleer",
        "Score",
        "Upper Deck",
        "O-Pee-Chee",
        "Leaf",
        "Select",
        "Prizm",
        "Mosaic",
        "Optic",
        "Hoops",
        "Stadium Club",
        "Other"
    ];

    private readonly InventoryOptions _options;
    private readonly IProductAccessService _productAccess;
    private readonly IDbContextFactory<InventoryDbContext> _dbContextFactory;
    private readonly IJsonMigrationService _migrationService;
    private readonly IFileScanner _scanner;
    private readonly IHashService _hashService;
    private readonly IOcrService _ocrService;
    private readonly IOcrLearningService _ocrLearningService;
    private readonly IImageEditService _imageEditService;
    private readonly IEbayExportService _exportService;
    private readonly IPricingService _pricingService;
    private readonly ILotBuilderService _lotBuilderService;
    private readonly IListingDraftService _listingDraftService;
    private readonly IListingAuditService _listingAuditService;
    private readonly IEbayConnectionService _ebayConnectionService;
    private readonly IRootManagementService _rootManagementService;
    private readonly IDiagnosticsBundleService _diagnosticsService;
    private readonly DatabaseMaintenanceService _databaseMaintenance;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        InventoryOptions options,
        IProductAccessService productAccess,
        IDbContextFactory<InventoryDbContext> dbContextFactory,
        IJsonMigrationService migrationService,
        IFileScanner scanner,
        IHashService hashService,
        IOcrService ocrService,
        IOcrLearningService ocrLearningService,
        IImageEditService imageEditService,
        IEbayExportService exportService,
        IPricingService pricingService,
        ILotBuilderService lotBuilderService,
        IListingDraftService listingDraftService,
        IListingAuditService listingAuditService,
        IEbayConnectionService ebayConnectionService,
        IRootManagementService rootManagementService,
        IDiagnosticsBundleService diagnosticsService,
        DatabaseMaintenanceService databaseMaintenance,
        ILogger<MainViewModel> logger)
    {
        _options = options;
        _productAccess = productAccess;
        _dbContextFactory = dbContextFactory;
        _migrationService = migrationService;
        _scanner = scanner;
        _hashService = hashService;
        _ocrService = ocrService;
        _ocrLearningService = ocrLearningService;
        _imageEditService = imageEditService;
        _exportService = exportService;
        _pricingService = pricingService;
        _lotBuilderService = lotBuilderService;
        _listingDraftService = listingDraftService;
        _listingAuditService = listingAuditService;
        _ebayConnectionService = ebayConnectionService;
        _rootManagementService = rootManagementService;
        _diagnosticsService = diagnosticsService;
        _databaseMaintenance = databaseMaintenance;
        _logger = logger;
        FilteredRows = CollectionViewSource.GetDefaultView(Rows);
        FilteredRows.Filter = FilterRow;
    }

    public ObservableCollection<PhotoGridRow> Rows { get; } = [];
    public ObservableCollection<PhotoGridRow> SelectedRows { get; } = [];
    public ObservableCollection<OcrCandidateRow> OcrCandidates { get; } = [];
    public ObservableCollection<WorkspaceRow> PricingRows { get; } = [];
    public ObservableCollection<WorkspaceRow> LotRows { get; } = [];
    public ObservableCollection<WorkspaceRow> ListingRows { get; } = [];
    public ObservableCollection<WorkspaceRow> AuditRows { get; } = [];
    public ObservableCollection<WorkspaceRow> JobRows { get; } = [];
    public ObservableCollection<WorkspaceRow> EbayRows { get; } = [];
    public ObservableCollection<WorkspaceRow> RootRows { get; } = [];
    public ObservableCollection<WorkspaceRow> EditRows { get; } = [];
    public ObservableCollection<string> CategoryOptions { get; } = [];
    public ObservableCollection<string> SportOrGameOptions { get; } = [];
    public ObservableCollection<string> BrandOptions { get; } = [];
    public ICollectionView FilteredRows { get; }

    public bool IsDeveloperBuild => _productAccess.Current.IsDeveloper;
    public Visibility DeveloperMenuVisibility => IsDeveloperBuild ? Visibility.Visible : Visibility.Collapsed;
    public string EditionText => IsDeveloperBuild ? "Development build — unlimited test access" : "Public release";
    public string AccessStatusText
    {
        get
        {
            var access = _productAccess.Current;
            if (access.IsDeveloper) return "Premium gates bypassed for development testing.";
            if (access.HasPaidEntitlement) return "Pro entitlement active.";
            if (!access.IsTrialActive) return "Trial ended — upgrade required for OCR, pricing, and exports.";
            var days = Math.Max(0, (int)Math.Ceiling((access.TrialEndsUtc - DateTimeOffset.UtcNow).TotalDays));
            return $"Free trial: {days} day(s), {access.PremiumActionsRemaining} premium action(s) remaining.";
        }
    }

    private bool TryUsePremiumFeature()
    {
        var allowed = _productAccess.TryConsumePremiumAction(out var message);
        OnPropertyChanged(nameof(AccessStatusText));
        if (!allowed) StatusText = message;
        return allowed;
    }

    [ObservableProperty]
    private PhotoGridRow? selectedRow;

    [ObservableProperty]
    private BitmapImage? previewImage;

    [ObservableProperty]
    private string statusText = "Ready.";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool filterNotListed;

    [ObservableProperty]
    private bool filterUnreviewedOcr;

    [ObservableProperty]
    private bool filterMissing;

    [ObservableProperty]
    private bool hideCurrentlyListed;

    [ObservableProperty]
    private bool hideFinishedListings;

    [ObservableProperty]
    private bool hideDoneTags;

    [ObservableProperty]
    private bool hideHiddenMetadataTerms;

    [ObservableProperty]
    private string hiddenMetadataTerms = string.Empty;

    [ObservableProperty]
    private bool useWorkflowSort = true;

    [ObservableProperty]
    private string ocrReviewText = string.Empty;

    [ObservableProperty]
    private string ocrCustomCropJson = string.Empty;

    [ObservableProperty]
    private string ocrRegionSummary = "No OCR region selected.";

    [ObservableProperty]
    private string metadataSelectionSummary = "No images selected.";

    [ObservableProperty]
    private string metadataItemName = string.Empty;

    [ObservableProperty]
    private string metadataCategory = string.Empty;

    [ObservableProperty]
    private string metadataSportOrGame = string.Empty;

    [ObservableProperty]
    private string metadataPlayerOrTitle = string.Empty;

    [ObservableProperty]
    private string metadataYear = string.Empty;

    [ObservableProperty]
    private string metadataBrand = string.Empty;

    [ObservableProperty]
    private string metadataSetName = string.Empty;

    [ObservableProperty]
    private string metadataCardNumber = string.Empty;

    [ObservableProperty]
    private string metadataSerialNumber = string.Empty;

    [ObservableProperty]
    private string metadataCondition = string.Empty;

    [ObservableProperty]
    private string metadataTags = string.Empty;

    [ObservableProperty]
    private string metadataNotes = string.Empty;

    [ObservableProperty]
    private bool metadataViewFront;

    [ObservableProperty]
    private bool metadataViewRear;

    partial void OnSelectedRowChanged(PhotoGridRow? value)
    {
        PreviewImage = value is null ? null : MainWindow.LoadBitmapNoLock(value.FullPath);
        OcrCustomCropJson = string.Empty;
        OcrRegionSummary = "No OCR region selected.";
        _ = LoadSelectionWorkspacesAsync(value);
        RefreshMetadataEditorFromSelection();
    }

    partial void OnSearchTextChanged(string value) => FilteredRows.Refresh();
    partial void OnFilterNotListedChanged(bool value) => FilteredRows.Refresh();
    partial void OnFilterUnreviewedOcrChanged(bool value) => FilteredRows.Refresh();
    partial void OnFilterMissingChanged(bool value) => FilteredRows.Refresh();
    partial void OnHideCurrentlyListedChanged(bool value) => FilteredRows.Refresh();
    partial void OnHideFinishedListingsChanged(bool value) => FilteredRows.Refresh();
    partial void OnHideDoneTagsChanged(bool value) => FilteredRows.Refresh();
    partial void OnHideHiddenMetadataTermsChanged(bool value) => FilteredRows.Refresh();
    partial void OnHiddenMetadataTermsChanged(string value) => FilteredRows.Refresh();
    partial void OnUseWorkflowSortChanged(bool value) => SortLoadedRows();
    partial void OnMetadataViewFrontChanged(bool value)
    {
        if (value)
        {
            MetadataViewRear = false;
        }
    }

    partial void OnMetadataViewRearChanged(bool value)
    {
        if (value)
        {
            MetadataViewFront = false;
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            StatusText = "Loading inventory...";
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var rows = await QueryRows(db).ToListAsync();
            await PopulateRowTagsAsync(db, rows);
            await PopulateMetadataOptionsAsync(db);
            rows = SortRows(rows).ToList();
            Rows.Clear();
            foreach (var row in rows)
            {
                Rows.Add(row);
            }

            StatusText = $"Loaded {Rows.Count} photo rows.";
            await LoadGlobalWorkspacesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory load failed");
            StatusText = "Load failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task DryRunMigrationAsync()
    {
        try
        {
            StatusText = "Running migration dry-run...";
            var report = await _migrationService.MigrateAsync(
                new MigrationOptions(_options.LegacyJsonPath, _options.DatabasePath, Apply: false, AllowLiveInventoryAccess: false),
                CancellationToken.None);
            StatusText = $"Dry-run: {report.PhotosRead} photos, {report.ItemsRead} groups, {report.MissingFiles} missing files.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration dry-run failed");
            StatusText = "Migration dry-run failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task ScanRootsAsync()
    {
        try
        {
            if (_options.DevelopmentSafeMode)
            {
                StatusText = "Scan is disabled in development safe mode. Relaunch with --allow-live to scan live roots.";
                return;
            }

            StatusText = "Scanning configured roots...";
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await EnsureDefaultRootsAsync(db);
            var roots = await db.SourceRoots.Where(r => r.Enabled).ToListAsync();
            var added = 0;
            var updated = 0;

            foreach (var root in roots)
            {
                await foreach (var scan in _scanner.ScanAsync(root, _options, CancellationToken.None))
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
            }

            await db.SaveChangesAsync();
            StatusText = $"Scan complete. Added {added}; updated {updated}.";
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
            StatusText = "Scan failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task OcrSelectedAsync()
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one photo first.";
            return;
        }
        if (!TryUsePremiumFeature()) return;
        await RunOcrForSelectedPhotoAsync(OcrProfile.TradingCardFront, string.Empty, "OCR");
    }

    [RelayCommand]
    private async Task OcrHighlightedRegionAsync()
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one photo first.";
            return;
        }
        if (string.IsNullOrWhiteSpace(OcrCustomCropJson))
        {
            StatusText = "Highlight a region on the preview image first.";
            return;
        }
        if (!TryUsePremiumFeature()) return;

        await RunOcrForSelectedPhotoAsync(OcrProfile.CustomRegion, OcrCustomCropJson, "Highlighted OCR");
    }

    public void SetOcrRegion(string cropJson, string summary)
    {
        OcrCustomCropJson = cropJson;
        OcrRegionSummary = summary;
        StatusText = summary + " Click OCR highlighted to read that region.";
    }

    private async Task RunOcrForSelectedPhotoAsync(OcrProfile profile, string customCropJson, string label)
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one photo first.";
            return;
        }
        if (!TryUsePremiumFeature()) return;

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var photo = await db.Photos.FindAsync(SelectedRow.PhotoId);
            if (photo is null)
            {
                StatusText = "Selected photo was not found in the database.";
                return;
            }

            StatusText = "Preparing OCR vocabulary...";
            var vocabulary = await db.InventoryItems
                .Select(i => string.Join(' ', i.PlayerOrTitle, i.Brand, i.SetName, i.SportOrGame, i.Author))
                .ToListAsync();
            await _ocrLearningService.RegenerateUserWordsAsync(vocabulary, CancellationToken.None);

            StatusText = "Running OCR...";
            var result = await _ocrService.RunAsync(
                new OcrRequest(photo.Id, photo.FullPath, profile, _options.OcrLanguage, _options.TesseractPath, _options.OcrUserWordsPath, _options.OcrTempRoot, customCropJson),
                CancellationToken.None);

            var run = new OcrRun
            {
                PhotoId = photo.Id,
                Profile = profile,
                BestText = result.BestText,
                AverageConfidence = result.AverageConfidence,
                StartedUtc = DateTimeOffset.UtcNow,
                CompletedUtc = DateTimeOffset.UtcNow,
                ErrorOutput = result.ErrorOutput
            };
            foreach (var candidate in result.Candidates.Take(20))
            {
                run.Candidates.Add(new OcrCandidate
                {
                    RecognizedText = candidate.Text,
                    RawTsv = candidate.RawTsv,
                    AverageConfidence = candidate.AverageConfidence,
                    WordConfidence = candidate.AverageConfidence,
                    Crop = candidate.Crop,
                    PreprocessingProfile = candidate.PreprocessingProfile,
                    PageSegmentationMode = candidate.PageSegmentationMode,
                    CandidateScore = candidate.Score,
                    ExecutionMilliseconds = candidate.ExecutionMilliseconds,
                    ErrorOutput = candidate.ErrorOutput,
                    DerivedImagePath = candidate.DerivedImagePath,
                    CropRectangleJson = candidate.CropRectangleJson,
                    RotationDegrees = candidate.RotationDegrees,
                    TransformMatrixJson = candidate.TransformMatrixJson,
                    WordBoxesJson = candidate.WordBoxesJson
                });

                if (!string.IsNullOrWhiteSpace(candidate.DerivedImagePath))
                {
                    db.OcrImageArtifacts.Add(new OcrImageArtifact
                    {
                        PhotoId = photo.Id,
                        OcrRun = run,
                        Kind = candidate.PreprocessingProfile.Contains("threshold", StringComparison.OrdinalIgnoreCase) ? OcrImageKind.Thresholded : OcrImageKind.Cropped,
                        OriginalImagePath = photo.FullPath,
                        DerivedImagePath = candidate.DerivedImagePath,
                        CropRectangleJson = candidate.CropRectangleJson,
                        RotationDegrees = candidate.RotationDegrees,
                        TransformMatrixJson = candidate.TransformMatrixJson,
                        PreprocessingProfile = candidate.PreprocessingProfile
                    });
                }
            }

            db.OcrRuns.Add(run);
            photo.OcrText = profile == OcrProfile.CustomRegion
                ? MergeOcrText(photo.OcrText, result.BestText)
                : result.BestText;
            photo.OcrConfidence = result.AverageConfidence;
            photo.OcrProfile = profile;
            photo.OcrReviewed = false;

            var link = await db.PhotoItemLinks
                .Include(l => l.InventoryItem)
                .FirstOrDefaultAsync(l => l.PhotoId == photo.Id);
            if (profile != OcrProfile.CustomRegion && link?.InventoryItem is { } item)
            {
                var metadataText = string.Join(Environment.NewLine, result.BestText, Path.GetFileNameWithoutExtension(photo.FileName));
                var analysis = CardMetadataAnalyzer.AnalyzeText(metadataText, photo.Id, result.AverageConfidence, _options.OcrMinimumConfidence);
                var appliedFields = ApplyHighConfidenceCardMetadata(item, analysis, _options.OcrMinimumConfidence);
                if (appliedFields.Count > 0)
                {
                    db.AuditEvents.Add(new AuditEvent
                    {
                        Operation = "ocr.metadata.applied",
                        PhotoId = photo.Id,
                        ItemId = item.Id,
                        Message = $"Applied OCR metadata fields: {string.Join(", ", appliedFields)}. Confidence {analysis.Confidence:0.##}%."
                    });
                }
            }

            await db.SaveChangesAsync();
            StatusText = $"{label} complete. {result.Candidates.Count} candidates, confidence {result.AverageConfidence:0.##}%.";
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR failed");
            StatusText = $"{label} failed: {ex.Message}";
        }
    }

    private static string MergeOcrText(string existingText, string additionalText)
    {
        if (string.IsNullOrWhiteSpace(existingText))
        {
            return additionalText.Trim();
        }

        if (string.IsNullOrWhiteSpace(additionalText))
        {
            return existingText.Trim();
        }

        var lines = existingText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Concat(additionalText.Split(["\r\n", "\n"], StringSplitOptions.None))
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(Environment.NewLine, lines);
    }

    [RelayCommand]
    private async Task SaveOcrReviewAsync()
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one photo first.";
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var photo = await db.Photos.FindAsync(SelectedRow.PhotoId);
        if (photo is null)
        {
            StatusText = "Selected photo was not found.";
            return;
        }

        var text = string.IsNullOrWhiteSpace(OcrReviewText) ? photo.OcrText : OcrReviewText.Trim();
        var runId = await db.OcrRuns.Where(r => r.PhotoId == photo.Id).OrderByDescending(r => r.CompletedUtc).Select(r => r.Id).FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(runId))
        {
            var reviewRun = new OcrRun
            {
                PhotoId = photo.Id,
                Profile = photo.OcrProfile,
                BestText = text,
                AverageConfidence = photo.OcrConfidence,
                StartedUtc = DateTimeOffset.UtcNow,
                CompletedUtc = DateTimeOffset.UtcNow
            };
            db.OcrRuns.Add(reviewRun);
            runId = reviewRun.Id;
        }

        db.OcrReviews.Add(new OcrReview
        {
            PhotoId = photo.Id,
            OcrRunId = runId,
            Status = OcrReviewStatus.Corrected,
            CorrectedText = text,
            Learned = false
        });
        photo.OcrText = text;
        photo.OcrReviewed = true;
        await db.SaveChangesAsync();
        StatusText = "OCR review saved.";
        await InitializeAsync();
    }

    [RelayCommand]
    private async Task SaveOcrAndLearnAsync()
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one photo first.";
            return;
        }

        await SaveOcrReviewAsync();
        await _ocrLearningService.SaveCorrectionAsync(SelectedRow.ItemName, OcrReviewText, "ocr-review", CancellationToken.None);
        StatusText = "OCR review saved and correction learning updated.";
    }

    [RelayCommand]
    private async Task RotateSelectedRightAsync()
    {
        await EditSelectedImageAsync([new ImageEditCommand(ImageEditOperationKind.RotateRight)]);
    }

    [RelayCommand]
    private async Task RotateSelectedLeftAsync()
    {
        await EditSelectedImageAsync([new ImageEditCommand(ImageEditOperationKind.RotateLeft)]);
    }

    [RelayCommand]
    private async Task CropSelectedCenterAsync()
    {
        await EditSelectedImageAsync([new ImageEditCommand(ImageEditOperationKind.Crop, "{\"x\":20,\"y\":20,\"width\":320,\"height\":448}")]);
    }

    [RelayCommand]
    private async Task FineRotateSelectedLeftAsync()
    {
        await EditSelectedImageAsync([new ImageEditCommand(ImageEditOperationKind.RotateArbitrary, "{\"degrees\":-1.0}")]);
    }

    [RelayCommand]
    private async Task FineRotateSelectedRightAsync()
    {
        await EditSelectedImageAsync([new ImageEditCommand(ImageEditOperationKind.RotateArbitrary, "{\"degrees\":1.0}")]);
    }

    [RelayCommand]
    private async Task StraightenSelectedAsync()
    {
        await EditSelectedImageAsync([new ImageEditCommand(ImageEditOperationKind.Straighten, "{\"degrees\":0.5}")]);
    }

    [RelayCommand]
    private async Task DedupeScanAsync()
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var photos = await db.Photos
                .OrderBy(photo => photo.FullPath)
                .ThenBy(photo => photo.ImportedUtc)
                .ThenBy(photo => photo.FileName)
                .ToListAsync();
            var hashedCount = 0;
            var missingCount = 0;
            var hashFailures = 0;

            foreach (var photo in photos.Where(photo => string.IsNullOrWhiteSpace(photo.Sha256)))
            {
                if (!File.Exists(photo.FullPath))
                {
                    photo.IsMissing = true;
                    missingCount++;
                    continue;
                }

                try
                {
                    photo.Sha256 = await _hashService.ComputeSha256Async(photo.FullPath, CancellationToken.None);
                    photo.IsMissing = false;
                    hashedCount++;
                }
                catch (Exception ex)
                {
                    hashFailures++;
                    _logger.LogWarning(ex, "Unable to hash photo during dedupe scan: {Path}", photo.FullPath);
                }
            }

            var duplicateCount = 0;
            var groupCount = 0;

            foreach (var group in photos
                .Where(photo => !string.IsNullOrWhiteSpace(photo.Sha256))
                .GroupBy(photo => photo.Sha256, StringComparer.OrdinalIgnoreCase))
            {
                var groupPhotos = group.ToList();
                if (groupPhotos.Count == 1)
                {
                    groupPhotos[0].IsDuplicate = false;
                    continue;
                }

                groupCount++;
                for (var index = 0; index < groupPhotos.Count; index++)
                {
                    groupPhotos[index].IsDuplicate = index > 0;
                    if (index > 0)
                    {
                        duplicateCount++;
                    }
                }
            }

            db.AuditEvents.Add(new AuditEvent
            {
                Operation = "dedupe.scan",
                Message = $"Hashed {hashedCount} image(s), marked {duplicateCount} duplicate image(s) across {groupCount} exact-hash group(s), missing {missingCount}, hash failures {hashFailures}."
            });
            await db.SaveChangesAsync();
            StatusText = $"Dedupe scan complete. Hashed {hashedCount}; marked {duplicateCount} duplicate(s) in {groupCount} group(s). Missing {missingCount}; hash failures {hashFailures}.";
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dedupe scan failed");
            StatusText = "Dedupe scan failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task ApplyMetadataToSelectedImagesAsync()
    {
        var selected = GetOrderedSelectedRows();
        if (selected.Count == 0)
        {
            StatusText = "Select one or more image rows first.";
            return;
        }
        if (!TryUsePremiumFeature()) return;

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var sourceRow = ChooseMetadataSourceRow(selected)
                ?? throw new InvalidOperationException("No source row could be resolved.");
            var item = await ResolveOrCreateSourceItemAsync(db, sourceRow);
            var linkResult = await EnsureRowsLinkedToItemAsync(db, item, selected);
            var appliedFields = await ApplyMetadataEditorFieldsAsync(db, item);
            var viewField = await ApplySelectedViewTypeAsync(db, item.Id, selected);
            if (!string.IsNullOrWhiteSpace(viewField))
            {
                appliedFields = appliedFields.Concat([viewField]).ToList();
            }

            item.ModifiedUtc = DateTimeOffset.UtcNow;
            db.AuditEvents.Add(new AuditEvent
            {
                Operation = "metadata.apply-to-images",
                ItemId = item.Id,
                PhotoId = sourceRow.PhotoId,
                Message = $"Applied item metadata to {linkResult.Linked} selected image(s); fields: {FieldSummary(appliedFields)}; added {linkResult.Added} link(s), moved {linkResult.Moved} existing link(s)."
            });

            await db.SaveChangesAsync();
            var organizeResult = await OrganizeItemPhotosAsync(db, item.Id, CancellationToken.None);
            StatusText = appliedFields.Count == 0
                ? $"Linked {linkResult.Linked} image(s) to {item.Name} metadata."
                : $"Applied {FieldSummary(appliedFields)} to {linkResult.Linked} image(s).";
            if (organizeResult.Attempted)
            {
                StatusText += $" Auto-sort moved {organizeResult.Moved}, copied {organizeResult.CopiedToEbayTemp} to eBay temp, skipped {organizeResult.Skipped}.";
            }

            await InitializeAsync();
            RefreshMetadataEditorFromSelection();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply metadata to selected images failed");
            StatusText = "Apply metadata failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task AutoSortCategorizedPhotosAsync()
    {
        if (_options.DevelopmentSafeMode)
        {
            StatusText = "Auto sort is disabled in development safe mode. Relaunch with --allow-live to move live inventory files.";
            return;
        }

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var itemIds = await db.InventoryItems
                .Where(item => item.Category != string.Empty)
                .OrderBy(item => item.Category)
                .ThenBy(item => item.Name)
                .Select(item => item.Id)
                .ToListAsync();

            var total = new OrganizePhotosResult(false, 0, 0, 0, 0);
            foreach (var itemId in itemIds)
            {
                var result = await OrganizeItemPhotosAsync(db, itemId, CancellationToken.None);
                total = new OrganizePhotosResult(
                    total.Attempted || result.Attempted,
                    total.Moved + result.Moved,
                    total.CopiedToEbayTemp + result.CopiedToEbayTemp,
                    total.Skipped + result.Skipped,
                    total.Missing + result.Missing);
            }

            StatusText = $"Auto sort complete. Moved {total.Moved}, copied {total.CopiedToEbayTemp} to eBay temp, skipped {total.Skipped}, missing {total.Missing}.";
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto sort failed");
            StatusText = "Auto sort failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private void ReloadMetadataEditor() => RefreshMetadataEditorFromSelection();

    [RelayCommand]
    private Task AssignAutoRoleCodesAsync() => AssignRoleCodesAsync(ImageRoleAssignmentMode.Auto);

    [RelayCommand]
    private Task AssignFrontRoleCodesAsync() => AssignRoleCodesAsync(ImageRoleAssignmentMode.Front);

    [RelayCommand]
    private Task AssignRearRoleCodesAsync() => AssignRoleCodesAsync(ImageRoleAssignmentMode.Rear);

    [RelayCommand]
    private Task AssignNextAdditionalRoleCodesAsync() => AssignRoleCodesAsync(ImageRoleAssignmentMode.NextAdditional);

    [RelayCommand]
    private Task AssignCornerRoleCodesAsync() => AssignRoleCodesAsync(ImageRoleAssignmentMode.Corner);

    [RelayCommand]
    private Task AssignDamageRoleCodesAsync() => AssignRoleCodesAsync(ImageRoleAssignmentMode.Damage);

    [RelayCommand]
    private async Task RecalculatePricingAsync()
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one item first.";
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var evidence = await db.PriceEvidence.Where(e => e.InventoryItemId == SelectedRow.ItemId).ToListAsync();
        var result = _pricingService.Compute(new PriceComputationRequest(evidence, 4.95m, 0.1325m));
        db.PriceSnapshots.Add(new PriceSnapshot
        {
            InventoryItemId = SelectedRow.ItemId,
            Low = result.Low,
            Median = result.Median,
            Average = result.Average,
            TrimmedAverage = result.TrimmedAverage,
            High = result.High,
            QuickSalePrice = result.QuickSalePrice,
            MarketPrice = result.MarketPrice,
            PremiumPrice = result.PremiumPrice,
            AuctionStart = result.AuctionStart,
            EstimatedFees = result.EstimatedFees,
            Shipping = result.Shipping,
            EstimatedNet = result.EstimatedNet,
            ComparableCount = result.ComparableCount,
            Confidence = result.Confidence,
            NeedsResearch = result.ComparableCount == 0
        });
        await db.SaveChangesAsync();
        StatusText = $"Pricing recalculated. Market {result.MarketPrice:C}; {result.ComparableCount} sold comparable(s).";
        await LoadSelectionWorkspacesAsync(SelectedRow);
    }

    [RelayCommand]
    private async Task CreateLotFromSelectionAsync()
    {
        var itemIds = SelectedRows.Count > 0 ? SelectedRows.Select(r => r.ItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() : [];
        if (itemIds.Count == 0)
        {
            StatusText = "Select item rows to build a lot.";
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var items = await db.InventoryItems.Where(i => itemIds.Contains(i.Id)).ToListAsync();
        var prices = await db.PriceSnapshots
            .Where(p => itemIds.Contains(p.InventoryItemId))
            .GroupBy(p => p.InventoryItemId)
            .Select(g => g.OrderByDescending(p => p.CreatedUtc).First())
            .ToDictionaryAsync(p => p.InventoryItemId);
        var result = _lotBuilderService.BuildLot(new LotBuildRequest($"Lot {DateTimeOffset.Now:yyyy-MM-dd HHmm}", items, prices));
        db.SaleLots.Add(result.Lot);
        await db.SaveChangesAsync();
        StatusText = $"Created lot with {result.Lot.Items.Count} items. Suggested BIN {result.Lot.SuggestedBuyItNow:C}.";
        await LoadGlobalWorkspacesAsync();
    }

    [RelayCommand]
    private async Task CreateListingDraftAsync()
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one item first.";
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var item = await db.InventoryItems.FindAsync(SelectedRow.ItemId);
        if (item is null)
        {
            StatusText = "Selected item was not found.";
            return;
        }

        var photos = await db.PhotoItemLinks.Where(l => l.InventoryItemId == item.Id).Include(l => l.Photo).OrderBy(l => l.SortOrder).Select(l => l.Photo!).ToListAsync();
        var price = await db.PriceSnapshots.Where(p => p.InventoryItemId == item.Id).OrderByDescending(p => p.CreatedUtc).FirstOrDefaultAsync();
        var listing = _listingDraftService.CreateDraft(new ListingDraftRequest(item, null, photos, price));
        db.MarketplaceListings.Add(listing);
        await db.SaveChangesAsync();
        StatusText = "Listing draft created with publishing disabled.";
        await LoadSelectionWorkspacesAsync(SelectedRow);
    }

    [RelayCommand]
    private async Task RunListingAuditAsync()
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one item first.";
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var listing = await db.MarketplaceListings.Where(l => l.InventoryItemId == SelectedRow.ItemId).OrderByDescending(l => l.CreatedUtc).FirstOrDefaultAsync();
        var item = await db.InventoryItems.FindAsync(SelectedRow.ItemId);
        if (listing is null || item is null)
        {
            StatusText = "Create a listing draft first.";
            return;
        }

        var photos = await db.PhotoItemLinks.Where(l => l.InventoryItemId == item.Id).Include(l => l.Photo).Select(l => l.Photo!).ToListAsync();
        var price = await db.PriceSnapshots.Where(p => p.InventoryItemId == item.Id).OrderByDescending(p => p.CreatedUtc).FirstOrDefaultAsync();
        var result = _listingAuditService.Audit(listing, item, photos, price);
        db.ListingAuditFindings.RemoveRange(db.ListingAuditFindings.Where(f => f.MarketplaceListingId == listing.Id));
        foreach (var finding in result.Findings)
        {
            finding.MarketplaceListingId = listing.Id;
            db.ListingAuditFindings.Add(finding);
        }

        await db.SaveChangesAsync();
        StatusText = result.BlocksPublishing ? "Listing audit found blockers." : "Listing audit complete.";
        await LoadSelectionWorkspacesAsync(SelectedRow);
    }

    [RelayCommand]
    private async Task RefreshEbayCapabilitiesAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var profile = await db.EbayConnectionProfiles.OrderByDescending(p => p.UpdatedUtc).FirstOrDefaultAsync()
            ?? new EbayConnectionProfile();
        EbayRows.Clear();
        foreach (var capability in _ebayConnectionService.GetCapabilities(profile))
        {
            EbayRows.Add(new WorkspaceRow
            {
                Name = capability.Name,
                Value = capability.Enabled ? "Enabled" : capability.CredentialRequired ? "Credential required" : "Disabled",
                Detail = capability.Reason
            });
        }

        StatusText = "eBay capability status refreshed.";
    }

    [RelayCommand]
    private async Task ExportSelectedAsync()
    {
        var selected = SelectedRows.Count > 0 ? SelectedRows.ToList() : SelectedRow is null ? [] : [SelectedRow];
        if (selected.Count == 0)
        {
            StatusText = "Select one or more rows to export.";
            return;
        }

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var itemIds = selected.Select(r => r.ItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var items = await db.InventoryItems
                .Include(i => i.ItemTags).ThenInclude(t => t.Tag)
                .Where(i => itemIds.Contains(i.Id))
                .ToListAsync();

            var photosByItem = new Dictionary<string, IReadOnlyList<Photo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var itemId in itemIds)
            {
                var photos = await db.PhotoItemLinks
                    .Where(l => l.InventoryItemId == itemId)
                    .Include(l => l.Photo)
                    .OrderBy(l => l.SortOrder)
                    .Select(l => l.Photo!)
                    .ToListAsync();
                photosByItem[itemId] = photos;
            }

            var result = await _exportService.ExportAsync(
                new ExportRequest(_options.EbayExportRoot, items, photosByItem, ListingStatus.Drafted),
                CancellationToken.None);

            StatusText = $"Exported {result.PhotoCount} photos to {result.ExportDirectory}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            StatusText = "Export failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task DatabaseCheckAsync()
    {
        try
        {
            var ok = await _databaseMaintenance.CheckIntegrityAsync(CancellationToken.None);
            StatusText = ok ? "SQLite integrity check passed." : "SQLite integrity check failed.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database check failed");
            StatusText = "Database check failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task CollectDiagnosticsAsync()
    {
        try
        {
            StatusText = "Collecting diagnostic bundle...";
            var result = await _diagnosticsService.CollectAsync(
                new DiagnosticsBundleRequest(_options.OperationsRoot),
                CancellationToken.None);
            StatusText = $"Diagnostic bundle: {result.ZipPath} ({result.SectionCount} sections).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics bundle failed");
            StatusText = "Diagnostics failed: " + ex.Message;
        }
    }

    public void RefreshMetadataEditorFromSelection()
    {
        var selected = GetOrderedSelectedRows();
        MetadataSelectionSummary = selected.Count switch
        {
            0 => "No images selected.",
            1 => "1 image selected.",
            _ => $"{selected.Count} images selected."
        };

        if (selected.Count == 0)
        {
            ClearMetadataEditor();
            return;
        }

        MetadataItemName = CommonValue(selected, row => row.ItemName);
        MetadataCategory = CommonValue(selected, row => row.Category);
        MetadataSportOrGame = CommonValue(selected, row => row.SportOrGame);
        MetadataPlayerOrTitle = CommonValue(selected, row => row.PlayerOrTitle);
        MetadataYear = CommonValue(selected, row => row.Year);
        MetadataBrand = CommonValue(selected, row => row.Brand);
        MetadataSetName = CommonValue(selected, row => row.SetName);
        MetadataCardNumber = CommonValue(selected, row => row.CardNumber);
        MetadataSerialNumber = CommonValue(selected, row => row.SerialNumber);
        MetadataCondition = CommonValue(selected, row => row.Condition);
        MetadataTags = CommonValue(selected, row => row.Tags);
        MetadataNotes = CommonValue(selected, row => row.Notes);
        MetadataViewFront = selected.All(row => string.Equals(row.ViewType, PhotoViewType.Front.ToString(), StringComparison.OrdinalIgnoreCase));
        MetadataViewRear = selected.All(row => string.Equals(row.ViewType, PhotoViewType.Back.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private void ClearMetadataEditor()
    {
        MetadataItemName = string.Empty;
        MetadataCategory = string.Empty;
        MetadataSportOrGame = string.Empty;
        MetadataPlayerOrTitle = string.Empty;
        MetadataYear = string.Empty;
        MetadataBrand = string.Empty;
        MetadataSetName = string.Empty;
        MetadataCardNumber = string.Empty;
        MetadataSerialNumber = string.Empty;
        MetadataCondition = string.Empty;
        MetadataTags = string.Empty;
        MetadataNotes = string.Empty;
        MetadataViewFront = false;
        MetadataViewRear = false;
    }

    private async Task PopulateMetadataOptionsAsync(InventoryDbContext db)
    {
        var categories = await db.InventoryItems
            .Select(item => item.Category)
            .Where(value => value != string.Empty)
            .Distinct()
            .ToListAsync();
        var sports = await db.InventoryItems
            .Select(item => item.SportOrGame)
            .Where(value => value != string.Empty)
            .Distinct()
            .ToListAsync();
        var brands = await db.InventoryItems
            .Select(item => item.Brand)
            .Where(value => value != string.Empty)
            .Distinct()
            .ToListAsync();

        ReplaceOptions(CategoryOptions, DefaultCategoryOptions.Concat(categories));
        ReplaceOptions(SportOrGameOptions, DefaultSportOrGameOptions.Concat(sports));
        ReplaceOptions(BrandOptions, DefaultBrandOptions.Concat(brands));
    }

    private static void ReplaceOptions(ObservableCollection<string> target, IEnumerable<string> values)
    {
        var next = values
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        target.Clear();
        foreach (var value in next)
        {
            target.Add(value);
        }
    }

    private PhotoGridRow? ChooseMetadataSourceRow(IReadOnlyList<PhotoGridRow> selected)
    {
        if (selected.Count == 0)
        {
            return SelectedRow;
        }

        if (SelectedRow is not null
            && selected.Any(row => string.Equals(row.PhotoId, SelectedRow.PhotoId, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(SelectedRow.ItemId))
        {
            return SelectedRow;
        }

        var existingItemRow = selected.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row.ItemId));
        if (existingItemRow is not null)
        {
            return existingItemRow;
        }

        return SelectedRow is not null
            && selected.Any(row => string.Equals(row.PhotoId, SelectedRow.PhotoId, StringComparison.OrdinalIgnoreCase))
                ? SelectedRow
                : selected[0];
    }

    private async Task<(int Linked, int Added, int Moved)> EnsureRowsLinkedToItemAsync(
        InventoryDbContext db,
        InventoryItem item,
        IReadOnlyList<PhotoGridRow> selected)
    {
        var selectedPhotoIds = selected
            .Select(row => row.PhotoId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var linksForSelectedPhotos = await db.PhotoItemLinks
            .Where(link => selectedPhotoIds.Contains(link.PhotoId))
            .ToListAsync();
        var existingItemLinks = await db.PhotoItemLinks
            .Where(link => link.InventoryItemId == item.Id)
            .ToListAsync();

        var nextSort = existingItemLinks.Count == 0 ? 0 : existingItemLinks.Max(link => link.SortOrder) + 1;
        var hasPrimary = existingItemLinks.Any(link => link.IsPrimary);
        var linked = 0;
        var added = 0;
        var moved = 0;

        foreach (var row in selected)
        {
            var photo = await db.Photos.FindAsync(row.PhotoId);
            if (photo is null)
            {
                continue;
            }

            var oldLinks = linksForSelectedPhotos
                .Where(link => link.PhotoId == photo.Id && link.InventoryItemId != item.Id)
                .ToList();
            if (oldLinks.Count > 0)
            {
                db.PhotoItemLinks.RemoveRange(oldLinks);
                moved += oldLinks.Count;
            }

            var link = linksForSelectedPhotos.FirstOrDefault(existing =>
                existing.PhotoId == photo.Id && existing.InventoryItemId == item.Id);
            if (link is null)
            {
                link = new PhotoItemLink
                {
                    PhotoId = photo.Id,
                    InventoryItemId = item.Id,
                    SortOrder = nextSort++
                };
                db.PhotoItemLinks.Add(link);
                linksForSelectedPhotos.Add(link);
                existingItemLinks.Add(link);
                added++;
            }

            if (!hasPrimary)
            {
                link.IsPrimary = true;
                hasPrimary = true;
            }

            var inferred = InferViewType(row, photo);
            if (link.ViewType == PhotoViewType.Unknown && inferred != PhotoViewType.Unknown)
            {
                link.ViewType = inferred;
            }

            if (photo.ViewType == PhotoViewType.Unknown && inferred != PhotoViewType.Unknown)
            {
                photo.ViewType = inferred;
            }

            linked++;
        }

        return (linked, added, moved);
    }

    private async Task<IReadOnlyList<string>> ApplyMetadataEditorFieldsAsync(InventoryDbContext db, InventoryItem item)
    {
        var applied = new List<string>();
        SetMetadataField(applied, nameof(InventoryItem.Name), MetadataItemName, item.Name, value => item.Name = value);
        SetMetadataField(applied, nameof(InventoryItem.Category), MetadataCategory, item.Category, value => item.Category = value);
        SetMetadataField(applied, nameof(InventoryItem.SportOrGame), MetadataSportOrGame, item.SportOrGame, value => item.SportOrGame = value);
        SetMetadataField(applied, nameof(InventoryItem.PlayerOrTitle), MetadataPlayerOrTitle, item.PlayerOrTitle, value => item.PlayerOrTitle = value);
        SetMetadataField(applied, nameof(InventoryItem.Year), MetadataYear, item.Year, value => item.Year = value);
        SetMetadataField(applied, nameof(InventoryItem.Brand), MetadataBrand, item.Brand, value => item.Brand = value);
        SetMetadataField(applied, nameof(InventoryItem.SetName), MetadataSetName, item.SetName, value => item.SetName = value);
        SetMetadataField(applied, nameof(InventoryItem.CardNumber), MetadataCardNumber, item.CardNumber, value => item.CardNumber = value);
        SetMetadataField(applied, nameof(InventoryItem.SerialNumber), MetadataSerialNumber, item.SerialNumber, value => item.SerialNumber = value);
        SetMetadataField(applied, nameof(InventoryItem.Condition), MetadataCondition, item.Condition, value => item.Condition = value);
        SetMetadataField(applied, nameof(InventoryItem.Notes), MetadataNotes, item.Notes, value => item.Notes = value);

        var tagNames = ParseTags(MetadataTags);
        if (tagNames.Count > 0 && await ReplaceItemTagsAsync(db, item.Id, tagNames))
        {
            applied.Add(nameof(InventoryItem.ItemTags));
        }

        if (applied.Count > 0)
        {
            item.ModifiedUtc = DateTimeOffset.UtcNow;
        }

        return applied;
    }

    private async Task<string> ApplySelectedViewTypeAsync(InventoryDbContext db, string itemId, IReadOnlyList<PhotoGridRow> selected)
    {
        var viewType = MetadataViewFront
            ? PhotoViewType.Front
            : MetadataViewRear
                ? PhotoViewType.Back
                : PhotoViewType.Unknown;
        if (viewType == PhotoViewType.Unknown)
        {
            return string.Empty;
        }

        var selectedPhotoIds = selected
            .Select(row => row.PhotoId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (selectedPhotoIds.Count == 0)
        {
            return string.Empty;
        }

        var photos = await db.Photos
            .Where(photo => selectedPhotoIds.Contains(photo.Id))
            .ToListAsync();
        var links = await db.PhotoItemLinks
            .Where(link => link.InventoryItemId == itemId && selectedPhotoIds.Contains(link.PhotoId))
            .ToListAsync();
        var changed = false;

        foreach (var photo in photos)
        {
            if (photo.ViewType != viewType)
            {
                photo.ViewType = viewType;
                changed = true;
            }
        }

        foreach (var link in links)
        {
            if (link.ViewType != viewType)
            {
                link.ViewType = viewType;
                changed = true;
            }
        }

        return changed ? nameof(Photo.ViewType) : string.Empty;
    }

    private async Task<OrganizePhotosResult> OrganizeItemPhotosAsync(InventoryDbContext db, string itemId, CancellationToken cancellationToken)
    {
        if (_options.DevelopmentSafeMode)
        {
            return new OrganizePhotosResult(false, 0, 0, 0, 0);
        }

        var item = await db.InventoryItems
            .Include(i => i.ItemTags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (item is null)
        {
            return new OrganizePhotosResult(false, 0, 0, 0, 0);
        }

        var tagNames = item.ItemTags
            .Select(link => link.Tag?.Name ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        if (!InventorySortTaxonomy.TryCreatePlan(item, tagNames, out var plan))
        {
            return new OrganizePhotosResult(false, 0, 0, 0, 0);
        }

        EnsureKnownSortDirectories();
        var links = await db.PhotoItemLinks
            .Where(link => link.InventoryItemId == item.Id)
            .Include(link => link.Photo)
            .OrderBy(link => link.SortOrder)
            .ToListAsync(cancellationToken);
        var reservedPaths = (await db.Photos
                .Select(photo => photo.FullPath)
                .Where(path => path != string.Empty)
                .ToListAsync(cancellationToken))
            .Select(SafeFullPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var targetDirectory = Path.Combine(_options.OperationsRoot, plan.RelativeDirectory);
        Directory.CreateDirectory(targetDirectory);

        var moved = 0;
        var copied = 0;
        var skipped = 0;
        var missing = 0;

        foreach (var link in links)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var photo = link.Photo;
            if (photo is null)
            {
                skipped++;
                continue;
            }

            var sourcePath = SafeFullPath(photo.FullPath);
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                photo.IsMissing = true;
                missing++;
                skipped++;
                continue;
            }

            try
            {
                reservedPaths.Remove(sourcePath);
                var destination = GetUniqueOrganizedPath(Path.Combine(targetDirectory, photo.FileName), reservedPaths, sourcePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                if (!PathsEqual(sourcePath, destination))
                {
                    File.Move(sourcePath, destination);
                    db.FileOperations.Add(new FileOperation
                    {
                        Kind = FileOperationKind.Organize,
                        Status = FileOperationStatus.Completed,
                        SourcePath = sourcePath,
                        DestinationPath = destination,
                        ReversalSourcePath = destination,
                        ReversalDestinationPath = sourcePath,
                        PhotoId = photo.Id,
                        CompletedUtc = DateTimeOffset.UtcNow
                    });
                    photo.FullPath = destination;
                    photo.FileName = Path.GetFileName(destination);
                    photo.Extension = Path.GetExtension(destination);
                    photo.ModifiedUtc = DateTimeOffset.UtcNow;
                    photo.IsMissing = false;
                    moved++;
                }

                reservedPaths.Add(photo.FullPath);

                if (plan.CopyToEbayTemp)
                {
                    var ebayPath = Path.Combine(_options.EbayExportRoot, Path.GetRelativePath(_options.OperationsRoot, photo.FullPath));
                    Directory.CreateDirectory(Path.GetDirectoryName(ebayPath)!);
                    File.Copy(photo.FullPath, ebayPath, overwrite: true);
                    db.FileOperations.Add(new FileOperation
                    {
                        Kind = FileOperationKind.ExportCopy,
                        Status = FileOperationStatus.Completed,
                        SourcePath = photo.FullPath,
                        DestinationPath = ebayPath,
                        PhotoId = photo.Id,
                        CompletedUtc = DateTimeOffset.UtcNow
                    });
                    copied++;
                }
            }
            catch (Exception ex)
            {
                skipped++;
                _logger.LogWarning(ex, "Unable to auto-sort photo {PhotoId} from {Path}", photo.Id, photo.FullPath);
                db.FileOperations.Add(new FileOperation
                {
                    Kind = FileOperationKind.Organize,
                    Status = FileOperationStatus.Failed,
                    SourcePath = sourcePath,
                    DestinationPath = targetDirectory,
                    PhotoId = photo.Id,
                    Error = ex.Message
                });
            }
        }

        db.AuditEvents.Add(new AuditEvent
        {
            Operation = "photos.auto-sort",
            ItemId = item.Id,
            Message = $"{plan.Reason} Moved {moved}, copied {copied} to eBay temp, skipped {skipped}, missing {missing}."
        });
        await db.SaveChangesAsync(cancellationToken);
        return new OrganizePhotosResult(true, moved, copied, skipped, missing);
    }

    private void EnsureKnownSortDirectories()
    {
        foreach (var relativeDirectory in InventorySortTaxonomy.GetKnownRelativeDirectories())
        {
            Directory.CreateDirectory(Path.Combine(_options.OperationsRoot, relativeDirectory));
        }
    }

    private static string GetUniqueOrganizedPath(string destination, HashSet<string> reservedPaths, string sourcePath)
    {
        var candidate = SafeFullPath(destination);
        var directory = Path.GetDirectoryName(candidate)!;
        var name = Path.GetFileNameWithoutExtension(candidate);
        var extension = Path.GetExtension(candidate);
        var index = 2;

        while ((!PathsEqual(candidate, sourcePath) && File.Exists(candidate)) ||
               (!PathsEqual(candidate, sourcePath) && reservedPaths.Contains(candidate)))
        {
            candidate = Path.Combine(directory, $"{name}_{index}{extension}");
            index++;
        }

        return candidate;
    }

    private static string SafeFullPath(string path)
    {
        try
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(SafeFullPath(left), SafeFullPath(right), StringComparison.OrdinalIgnoreCase);

    private sealed record OrganizePhotosResult(
        bool Attempted,
        int Moved,
        int CopiedToEbayTemp,
        int Skipped,
        int Missing);

    private static async Task<bool> ReplaceItemTagsAsync(InventoryDbContext db, string itemId, IReadOnlyList<string> tagNames)
    {
        var existingLinks = await db.ItemTags
            .Include(link => link.Tag)
            .Where(link => link.InventoryItemId == itemId)
            .ToListAsync();
        var existingNames = existingLinks
            .Select(link => link.Tag?.Name ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (existingNames.SetEquals(tagNames))
        {
            return false;
        }

        db.ItemTags.RemoveRange(existingLinks);
        var knownTags = await db.Tags.ToListAsync();
        foreach (var tagName in tagNames)
        {
            var tag = knownTags.FirstOrDefault(existing =>
                existing.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                tag = new Tag { Name = tagName };
                db.Tags.Add(tag);
                knownTags.Add(tag);
            }

            db.ItemTags.Add(new ItemTag { InventoryItemId = itemId, TagId = tag.Id, Tag = tag });
        }

        return true;
    }

    private static void SetMetadataField(
        ICollection<string> applied,
        string fieldName,
        string editedValue,
        string currentValue,
        Action<string> setter)
    {
        var value = editedValue.Trim();
        if (string.IsNullOrWhiteSpace(value) || string.Equals(currentValue, value, StringComparison.Ordinal))
        {
            return;
        }

        setter(value);
        applied.Add(fieldName);
    }

    private static IReadOnlyList<string> ParseTags(string value) =>
        value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string CommonValue(IReadOnlyList<PhotoGridRow> selected, Func<PhotoGridRow, string> selector)
    {
        var values = selected
            .Select(selector)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return values.Count == 1 ? values[0] : string.Empty;
    }

    private static string FieldSummary(IReadOnlyList<string> fields) =>
        fields.Count == 0 ? "none" : string.Join(", ", fields);

    private static async Task PopulateRowTagsAsync(InventoryDbContext db, IReadOnlyList<PhotoGridRow> rows)
    {
        var itemIds = rows
            .Select(row => row.ItemId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (itemIds.Count == 0)
        {
            return;
        }

        var itemTags = await db.ItemTags
            .Include(link => link.Tag)
            .Where(link => itemIds.Contains(link.InventoryItemId))
            .ToListAsync();
        var tagsByItem = itemTags
            .Where(link => link.Tag is not null)
            .GroupBy(link => link.InventoryItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group
                    .Select(link => link.Tag!.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (tagsByItem.TryGetValue(row.ItemId, out var tags))
            {
                row.Tags = tags;
            }
        }
    }

    private bool FilterRow(object value)
    {
        if (value is not PhotoGridRow row)
        {
            return false;
        }

        if (FilterNotListed && row.ListingStatus != ListingStatus.NotListed)
        {
            return false;
        }

        if (FilterUnreviewedOcr && row.OcrReviewed)
        {
            return false;
        }

        if (FilterMissing && !row.IsMissing)
        {
            return false;
        }

        if (HideCurrentlyListed && IsCurrentlyListed(row))
        {
            return false;
        }

        if (HideFinishedListings && IsFinishedListingStatus(row.ListingStatus))
        {
            return false;
        }

        if (HideDoneTags && HasAnyMetadataTerm(row, ["done", "complete", "completed", "finished"]))
        {
            return false;
        }

        if (HideHiddenMetadataTerms && HiddenTerms().Any(term => HasAnyMetadataTerm(row, [term])))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            return BuildMetadataHaystack(row).Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private void SortLoadedRows()
    {
        if (Rows.Count <= 1)
        {
            return;
        }

        var selectedPhotoId = SelectedRow?.PhotoId;
        var selectedPhotoIds = SelectedRows.Select(row => row.PhotoId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sorted = SortRows(Rows).ToList();
        Rows.Clear();
        foreach (var row in sorted)
        {
            Rows.Add(row);
        }

        if (!string.IsNullOrWhiteSpace(selectedPhotoId))
        {
            SelectedRow = Rows.FirstOrDefault(row => string.Equals(row.PhotoId, selectedPhotoId, StringComparison.OrdinalIgnoreCase));
        }

        SelectedRows.Clear();
        foreach (var row in Rows.Where(row => selectedPhotoIds.Contains(row.PhotoId)))
        {
            SelectedRows.Add(row);
        }

        FilteredRows.Refresh();
    }

    private IEnumerable<PhotoGridRow> SortRows(IEnumerable<PhotoGridRow> rows)
    {
        return UseWorkflowSort
            ? rows
                .OrderBy(row => row.IsMissing ? 1 : 0)
                .ThenBy(row => ListingStatusWorkflowRank(row.ListingStatus))
                .ThenBy(row => row.OcrReviewed ? 1 : 0)
                .ThenBy(row => string.IsNullOrWhiteSpace(row.ItemName) ? 1 : 0)
                .ThenBy(row => row.ItemName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => RoleSortOrder(row))
                .ThenBy(row => row.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.FullPath, StringComparer.OrdinalIgnoreCase)
            : rows
                .OrderBy(row => row.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.FullPath, StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyList<string> HiddenTerms() =>
        HiddenMetadataTerms.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsCurrentlyListed(PhotoGridRow row) =>
        row.ListingStatus is ListingStatus.CurrentlyListed or ListingStatus.Active
        || HasAnyMetadataTerm(row, ["currently listed", "currentlylisted", "active listing"]);

    private static bool IsFinishedListingStatus(ListingStatus status) =>
        status is ListingStatus.Listed
            or ListingStatus.Sold
            or ListingStatus.Ended
            or ListingStatus.Archived
            or ListingStatus.Cancelled;

    private static bool HasAnyMetadataTerm(PhotoGridRow row, IReadOnlyList<string> terms)
    {
        var haystack = BuildMetadataHaystack(row);
        return terms.Any(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildMetadataHaystack(PhotoGridRow row) =>
        string.Join(' ',
            row.FileName,
            row.ItemName,
            row.ListingStatus,
            row.Category,
            row.SportOrGame,
            row.PlayerOrTitle,
            row.Year,
            row.Brand,
            row.SetName,
            row.SerialNumber,
            row.CardNumber,
            row.Condition,
            row.Tags,
            row.Notes,
            row.FullPath);

    private static int ListingStatusWorkflowRank(ListingStatus status) => status switch
    {
        ListingStatus.Error => 0,
        ListingStatus.NotListed => 1,
        ListingStatus.ReadyToList or ListingStatus.Ready => 2,
        ListingStatus.Draft or ListingStatus.Drafted => 3,
        ListingStatus.Scheduled => 4,
        ListingStatus.Active or ListingStatus.CurrentlyListed => 8,
        ListingStatus.Listed => 9,
        ListingStatus.Sold or ListingStatus.Ended => 10,
        ListingStatus.Cancelled or ListingStatus.Archived => 11,
        _ => 7
    };

    private static int RoleSortOrder(PhotoGridRow row)
    {
        var code = row.ImageRoleCode.Trim();
        if (!string.IsNullOrWhiteSpace(code))
        {
            if (TryNormalRoleSort(code, out var normalSort))
            {
                return normalSort;
            }

            if (TrySpecialRoleSort(code, "CS", 300, out var cornerSort))
            {
                return cornerSort;
            }

            if (TrySpecialRoleSort(code, "Dd", 400, out var damageSort))
            {
                return damageSort;
            }
        }

        return Enum.TryParse<PhotoViewType>(row.ViewType, out var viewType)
            ? viewType switch
            {
                PhotoViewType.Front => 0,
                PhotoViewType.Back => 100,
                PhotoViewType.CardNumber or PhotoViewType.SerialNumber => 250,
                PhotoViewType.ConditionCloseup
                    or PhotoViewType.FrontUpperLeft
                    or PhotoViewType.FrontUpperRight
                    or PhotoViewType.FrontLowerLeft
                    or PhotoViewType.FrontLowerRight
                    or PhotoViewType.BackUpperLeft
                    or PhotoViewType.BackUpperRight
                    or PhotoViewType.BackLowerLeft
                    or PhotoViewType.BackLowerRight => 300,
                PhotoViewType.Other => 500,
                _ => 900
            }
            : 900;
    }

    private static bool TryNormalRoleSort(string code, out int sort)
    {
        sort = 0;
        if (code.Length == 0 || !char.IsLetter(code[0]))
        {
            return false;
        }

        var prefix = char.ToUpperInvariant(code[0]);
        if (prefix is < 'A' or > 'Z')
        {
            return false;
        }

        var suffix = code[1..];
        var index = 0;
        if (suffix.Length > 0 && !int.TryParse(suffix, out index))
        {
            return false;
        }

        sort = ((prefix - 'A') * 100) + index;
        return true;
    }

    private static bool TrySpecialRoleSort(string code, string prefix, int baseSort, out int sort)
    {
        sort = 0;
        if (!code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suffix = code[prefix.Length..];
        if (!int.TryParse(suffix, out var index))
        {
            return false;
        }

        sort = baseSort + index;
        return true;
    }

    private static IQueryable<PhotoGridRow> QueryRows(InventoryDbContext db) =>
        from photo in db.Photos
        join link in db.PhotoItemLinks on photo.Id equals link.PhotoId into links
        from link in links.DefaultIfEmpty()
        join item in db.InventoryItems on link.InventoryItemId equals item.Id into items
        from item in items.DefaultIfEmpty()
        orderby item.Name, link.SortOrder, photo.FileName
        select new PhotoGridRow
        {
            PhotoId = photo.Id,
            ItemId = item == null ? string.Empty : item.Id,
            FileName = photo.FileName,
            ItemName = item == null ? string.Empty : item.Name,
            ImageRoleCode = link == null ? string.Empty : link.ImageRoleCode,
            ViewType = photo.ViewType.ToString(),
            ListingStatus = item == null ? ListingStatus.NotListed : item.ListingStatus,
            Category = item == null ? string.Empty : item.Category,
            SportOrGame = item == null ? string.Empty : item.SportOrGame,
            PlayerOrTitle = item == null ? string.Empty : item.PlayerOrTitle,
            Year = item == null ? string.Empty : item.Year,
            Brand = item == null ? string.Empty : item.Brand,
            SetName = item == null ? string.Empty : item.SetName,
            SerialNumber = item == null ? string.Empty : item.SerialNumber,
            CardNumber = item == null ? string.Empty : item.CardNumber,
            Condition = item == null ? string.Empty : item.Condition,
            Tags = string.Empty,
            Notes = item == null ? string.Empty : item.Notes,
            OcrConfidence = photo.OcrConfidence,
            OcrReviewed = photo.OcrReviewed,
            FullPath = photo.FullPath,
            IsMissing = photo.IsMissing,
            IsDuplicate = photo.IsDuplicate
        };

    private async Task EnsureDefaultRootsAsync(InventoryDbContext db)
    {
        var roots = new[]
        {
            _options.DefaultSourceRoot,
            Path.Combine(_options.OperationsRoot, "cards"),
            Path.Combine(_options.OperationsRoot, "inbox"),
            Path.Combine(_options.OperationsRoot, "library")
        };

        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await db.SourceRoots.AnyAsync(r => r.Path == root))
            {
                db.SourceRoots.Add(new SourceRoot { Path = root, Enabled = !_options.DevelopmentSafeMode && Directory.Exists(root), IndexInPlace = true, Recursive = true });
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task EditSelectedImageAsync(IReadOnlyList<ImageEditCommand> commands)
    {
        if (SelectedRow is null)
        {
            StatusText = "Select one photo first.";
            return;
        }

        var result = await _imageEditService.ApplyAsync(new ImageEditRequest(SelectedRow.PhotoId, SelectedRow.FullPath, Path.Combine(_options.OperationsRoot, "image-edits"), commands), CancellationToken.None);
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var session = new ImageEditSession
        {
            PhotoId = SelectedRow.PhotoId,
            SourcePath = SelectedRow.FullPath,
            CurrentDerivedPath = result.OutputPath,
            CurrentOperationIndex = result.Operations.Count - 1
        };
        foreach (var operation in result.Operations)
        {
            session.Operations.Add(operation);
        }

        db.ImageEditSessions.Add(session);
        await db.SaveChangesAsync();
        StatusText = $"Saved derived edit copy: {result.OutputPath}";
        await LoadSelectionWorkspacesAsync(SelectedRow);
    }

    private async Task AssignRoleCodesAsync(ImageRoleAssignmentMode mode)
    {
        var selected = GetOrderedSelectedRows();
        if (SelectedRow is null || selected.Count == 0)
        {
            StatusText = "Select one or more image rows first.";
            return;
        }

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var item = await ResolveOrCreateSourceItemAsync(db, SelectedRow);
            var selectedPhotoIds = selected.Select(row => row.PhotoId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingLinks = await db.PhotoItemLinks
                .Where(link => link.InventoryItemId == item.Id)
                .ToListAsync();
            var usedCodes = existingLinks
                .Where(link => !selectedPhotoIds.Contains(link.PhotoId))
                .Select(link => link.ImageRoleCode)
                .Where(ImageRoleCodeGenerator.IsValid)
                .Select(ImageRoleCodeGenerator.Normalize)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var labels = BuildRoleCodeAssignments(mode, selected, usedCodes);
            var nextSort = existingLinks.Count == 0 ? 0 : existingLinks.Max(link => link.SortOrder) + 1;

            for (var index = 0; index < selected.Count; index++)
            {
                var row = selected[index];
                var photo = await db.Photos.FindAsync(row.PhotoId);
                if (photo is null)
                {
                    continue;
                }

                var oldLinks = await db.PhotoItemLinks
                    .Where(link => link.PhotoId == photo.Id && link.InventoryItemId != item.Id)
                    .ToListAsync();
                if (oldLinks.Count > 0)
                {
                    db.PhotoItemLinks.RemoveRange(oldLinks);
                }

                var link = await db.PhotoItemLinks.FindAsync(photo.Id, item.Id);
                if (link is null)
                {
                    link = new PhotoItemLink
                    {
                        PhotoId = photo.Id,
                        InventoryItemId = item.Id,
                        SortOrder = nextSort++
                    };
                    db.PhotoItemLinks.Add(link);
                }

                var label = labels[index];
                var viewType = mode switch
                {
                    ImageRoleAssignmentMode.Front => PhotoViewType.Front,
                    ImageRoleAssignmentMode.Rear => PhotoViewType.Back,
                    ImageRoleAssignmentMode.Corner or ImageRoleAssignmentMode.Damage => PhotoViewType.ConditionCloseup,
                    ImageRoleAssignmentMode.NextAdditional => PhotoViewType.Other,
                    _ => InferViewType(row, photo)
                };

                link.ImageRoleCode = label;
                link.ViewType = viewType;
                link.IsPrimary = label.Equals("A", StringComparison.OrdinalIgnoreCase);
                if (link.IsPrimary)
                {
                    foreach (var other in existingLinks.Where(other => other.InventoryItemId == item.Id && other.PhotoId != link.PhotoId))
                    {
                        other.IsPrimary = false;
                    }
                }

                if (photo.ViewType == PhotoViewType.Unknown || mode != ImageRoleAssignmentMode.Auto)
                {
                    photo.ViewType = viewType;
                }
            }

            item.ModifiedUtc = DateTimeOffset.UtcNow;
            db.AuditEvents.Add(new AuditEvent
            {
                Operation = "image-role-codes.assign",
                ItemId = item.Id,
                PhotoId = SelectedRow.PhotoId,
                Message = $"Assigned {mode} image role labels to {selected.Count} image(s): {string.Join(", ", labels)}."
            });

            await db.SaveChangesAsync();
            StatusText = $"Assigned labels: {string.Join(", ", labels)}.";
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image role code assignment failed");
            StatusText = "Image label assignment failed: " + ex.Message;
        }
    }

    private IReadOnlyList<PhotoGridRow> GetOrderedSelectedRows()
    {
        var selected = SelectedRows.Count > 0
            ? SelectedRows.ToList()
            : SelectedRow is null ? [] : [SelectedRow];

        return selected
            .DistinctBy(row => row.PhotoId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(row =>
            {
                var index = Rows.IndexOf(row);
                return index < 0 ? int.MaxValue : index;
            })
            .ThenBy(row => row.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<InventoryItem> ResolveOrCreateSourceItemAsync(InventoryDbContext db, PhotoGridRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.ItemId))
        {
            var existing = await db.InventoryItems.FindAsync(row.ItemId);
            if (existing is not null)
            {
                return existing;
            }
        }

        var photo = await db.Photos.FindAsync(row.PhotoId)
            ?? throw new InvalidOperationException("Source photo was not found in the database.");
        var analysisText = string.Join(Environment.NewLine, photo.OcrText, Path.GetFileNameWithoutExtension(photo.FileName));
        var analysis = CardMetadataAnalyzer.AnalyzeText(analysisText, photo.Id, photo.OcrConfidence, 0);
        var item = analysis.Candidate;
        item.Name = string.IsNullOrWhiteSpace(item.Name)
            ? FilenameSanitizer.Sanitize(Path.GetFileNameWithoutExtension(photo.FileName), 120, "Card")
            : item.Name;
        item.ListingStatus = ListingStatus.NotListed;
        item.CreatedUtc = DateTimeOffset.UtcNow;
        item.ModifiedUtc = DateTimeOffset.UtcNow;

        db.InventoryItems.Add(item);
        var inferred = photo.ViewType == PhotoViewType.Unknown
            ? PhotoPairingAnalyzer.InferViewTypeFromFileName(photo.FileName)
            : photo.ViewType;
        db.PhotoItemLinks.Add(new PhotoItemLink
        {
            PhotoId = photo.Id,
            InventoryItemId = item.Id,
            SortOrder = 0,
            ViewType = inferred,
            IsPrimary = true
        });
        if (photo.ViewType == PhotoViewType.Unknown && inferred != PhotoViewType.Unknown)
        {
            photo.ViewType = inferred;
        }

        await db.SaveChangesAsync();
        return item;
    }

    private static IReadOnlyList<string> BuildRoleCodeAssignments(
        ImageRoleAssignmentMode mode,
        IReadOnlyList<PhotoGridRow> selected,
        HashSet<string> usedCodes)
    {
        return mode switch
        {
            ImageRoleAssignmentMode.Front => AssignNormal('A', selected.Count, usedCodes),
            ImageRoleAssignmentMode.Rear => AssignNormal('B', selected.Count, usedCodes),
            ImageRoleAssignmentMode.NextAdditional => AssignNormal(ImageRoleCodeGenerator.NextAdditionalPrefix(usedCodes), selected.Count, usedCodes),
            ImageRoleAssignmentMode.Corner => AssignSpecial("CS", selected.Count, 8, usedCodes),
            ImageRoleAssignmentMode.Damage => AssignSpecial("Dd", selected.Count, 10, usedCodes),
            _ => AssignAuto(selected, usedCodes)
        };
    }

    private static IReadOnlyList<string> AssignAuto(IReadOnlyList<PhotoGridRow> selected, HashSet<string> usedCodes)
    {
        var labels = new List<string>();
        char? additionalPrefix = null;
        foreach (var row in selected)
        {
            var viewType = Enum.TryParse<PhotoViewType>(row.ViewType, out var parsed) ? parsed : PhotoViewType.Unknown;
            if (viewType == PhotoViewType.Unknown)
            {
                viewType = PhotoPairingAnalyzer.InferViewTypeFromFileName(row.FileName);
            }

            var prefix = viewType switch
            {
                PhotoViewType.Front => 'A',
                PhotoViewType.Back => 'B',
                _ => additionalPrefix ??= ImageRoleCodeGenerator.NextAdditionalPrefix(usedCodes)
            };
            labels.Add(NextNormalCode(prefix, usedCodes));
        }

        return labels;
    }

    private static IReadOnlyList<string> AssignNormal(char prefix, int count, HashSet<string> usedCodes)
    {
        var labels = new List<string>();
        for (var i = 0; i < count; i++)
        {
            labels.Add(NextNormalCode(prefix, usedCodes));
        }

        return labels;
    }

    private static string NextNormalCode(char prefix, HashSet<string> usedCodes)
    {
        var normalizedPrefix = char.ToUpperInvariant(prefix);
        var baseLabel = normalizedPrefix.ToString();
        if (usedCodes.Add(baseLabel))
        {
            return baseLabel;
        }

        for (var index = 1; index < 10_000; index++)
        {
            var label = normalizedPrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (usedCodes.Add(label))
            {
                return label;
            }
        }

        throw new InvalidOperationException($"Could not allocate image role code for prefix {prefix}.");
    }

    private static IReadOnlyList<string> AssignSpecial(string prefix, int count, int max, HashSet<string> usedCodes)
    {
        var labels = new List<string>();
        for (var number = 1; number <= max && labels.Count < count; number++)
        {
            var label = prefix + number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var normalized = ImageRoleCodeGenerator.Normalize(label);
            if (usedCodes.Add(normalized))
            {
                labels.Add(normalized);
            }
        }

        if (labels.Count != count)
        {
            throw new InvalidOperationException(prefix.Equals("CS", StringComparison.OrdinalIgnoreCase)
                ? "Corner shot labels support only CS1 through CS8 for one item."
                : "Damage documentation labels support only Dd1 through Dd10 for one item.");
        }

        return labels;
    }

    private static PhotoViewType InferViewType(PhotoGridRow row, Photo photo)
    {
        if (Enum.TryParse<PhotoViewType>(row.ViewType, out var parsed) && parsed != PhotoViewType.Unknown)
        {
            return parsed;
        }

        if (photo.ViewType != PhotoViewType.Unknown)
        {
            return photo.ViewType;
        }

        return PhotoPairingAnalyzer.InferViewTypeFromFileName(photo.FileName);
    }

    private static async Task<int> NextSortOrderAsync(InventoryDbContext db, string itemId)
    {
        var max = await db.PhotoItemLinks
            .Where(link => link.InventoryItemId == itemId)
            .Select(link => (int?)link.SortOrder)
            .MaxAsync();
        return (max ?? -1) + 1;
    }

    private async Task LoadSelectionWorkspacesAsync(PhotoGridRow? row)
    {
        if (row is null)
        {
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        OcrCandidates.Clear();
        var candidates = await db.OcrRuns
            .Where(r => r.PhotoId == row.PhotoId)
            .OrderByDescending(r => r.CompletedUtc)
            .SelectMany(r => r.Candidates)
            .OrderByDescending(c => c.CandidateScore)
            .Take(25)
            .ToListAsync();
        foreach (var candidate in candidates)
        {
            OcrCandidates.Add(new OcrCandidateRow
            {
                Id = candidate.Id,
                Text = candidate.RecognizedText,
                Confidence = candidate.AverageConfidence,
                Score = candidate.CandidateScore,
                Profile = candidate.PreprocessingProfile,
                DerivedImagePath = candidate.DerivedImagePath
            });
        }

        OcrReviewText = await db.Photos.Where(p => p.Id == row.PhotoId).Select(p => p.OcrText).FirstOrDefaultAsync() ?? string.Empty;

        PricingRows.Clear();
        var price = await db.PriceSnapshots.Where(p => p.InventoryItemId == row.ItemId).OrderByDescending(p => p.CreatedUtc).FirstOrDefaultAsync();
        if (price is not null)
        {
            Add(PricingRows, "Market", price.MarketPrice.ToString("C"), $"{price.ComparableCount} comparable(s), {price.Confidence}");
            Add(PricingRows, "Quick sale", price.QuickSalePrice.ToString("C"), "85% of market price");
            Add(PricingRows, "Estimated net", price.EstimatedNet.ToString("C"), $"Fees {price.EstimatedFees:C}, shipping {price.Shipping:C}");
        }

        ListingRows.Clear();
        foreach (var listing in await db.MarketplaceListings.Where(l => l.InventoryItemId == row.ItemId).OrderByDescending(l => l.CreatedUtc).Take(10).ToListAsync())
        {
            Add(ListingRows, listing.Status.ToString(), listing.Title, listing.PublishingEnabled ? "Publishing enabled" : "Publishing disabled");
        }

        AuditRows.Clear();
        var listingIds = await db.MarketplaceListings.Where(l => l.InventoryItemId == row.ItemId).Select(l => l.Id).ToListAsync();
        foreach (var finding in await db.ListingAuditFindings.Where(f => listingIds.Contains(f.MarketplaceListingId)).OrderByDescending(f => f.CreatedUtc).Take(25).ToListAsync())
        {
            Add(AuditRows, finding.Severity.ToString(), finding.RuleId, finding.Message);
        }

        EditRows.Clear();
        foreach (var edit in await db.ImageEditSessions.Where(e => e.PhotoId == row.PhotoId).OrderByDescending(e => e.ModifiedUtc).Take(10).ToListAsync())
        {
            Add(EditRows, edit.ModifiedUtc.LocalDateTime.ToString("g"), edit.CurrentDerivedPath, $"{edit.CurrentOperationIndex + 1} operation(s)");
        }
    }

    private async Task LoadGlobalWorkspacesAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        RootRows.Clear();
        foreach (var root in await db.SourceRoots.OrderBy(r => r.Path).ToListAsync())
        {
            var validation = _rootManagementService.ValidateRoot(root.Path, await db.SourceRoots.Where(r => r.Id != root.Id).ToListAsync());
            Add(RootRows, root.Enabled ? "Enabled" : "Disabled", validation.NormalizedPath, validation.Message);
        }

        JobRows.Clear();
        foreach (var job in await db.Jobs.OrderByDescending(j => j.CreatedUtc).Take(20).ToListAsync())
        {
            Add(JobRows, job.Kind.ToString(), job.Status.ToString(), $"{job.Progress:0.#}% {job.CurrentFile}");
        }

        LotRows.Clear();
        foreach (var lot in await db.SaleLots.Include(l => l.Items).OrderByDescending(l => l.ModifiedUtc).Take(20).ToListAsync())
        {
            Add(LotRows, lot.Status.ToString(), lot.Name, $"{lot.Items.Count} item(s), BIN {lot.SuggestedBuyItNow:C}, net {lot.EstimatedNet:C}");
        }

        await RefreshEbayCapabilitiesAsync();
    }

    private static void Add(ObservableCollection<WorkspaceRow> rows, string name, string value, string detail) =>
        rows.Add(new WorkspaceRow { Name = name, Value = value, Detail = detail });

    private enum ImageRoleAssignmentMode
    {
        Auto,
        Front,
        Rear,
        NextAdditional,
        Corner,
        Damage
    }

    private static IReadOnlyList<string> ApplyHighConfidenceCardMetadata(InventoryItem item, CardMetadataAnalysis analysis, double minimumConfidence)
    {
        if (analysis.Confidence < minimumConfidence)
        {
            return [];
        }

        var source = analysis.Candidate;
        var applied = new List<string>();
        SetIfBlank(nameof(InventoryItem.Category), item.Category, source.Category, value => item.Category = value);
        SetIfBlank(nameof(InventoryItem.SportOrGame), item.SportOrGame, source.SportOrGame, value => item.SportOrGame = value);
        SetIfBlank(nameof(InventoryItem.PlayerOrTitle), item.PlayerOrTitle, source.PlayerOrTitle, value => item.PlayerOrTitle = value);
        SetIfBlank(nameof(InventoryItem.Year), item.Year, source.Year, value => item.Year = value);
        SetIfBlank(nameof(InventoryItem.Brand), item.Brand, source.Brand, value => item.Brand = value);
        SetIfBlank(nameof(InventoryItem.Manufacturer), item.Manufacturer, source.Manufacturer, value => item.Manufacturer = value);
        SetIfBlank(nameof(InventoryItem.CardNumber), item.CardNumber, source.CardNumber, value => item.CardNumber = value);
        SetIfBlank(nameof(InventoryItem.SerialNumber), item.SerialNumber, source.SerialNumber, value => item.SerialNumber = value);
        SetIfBlank(nameof(InventoryItem.GradingCompany), item.GradingCompany, source.GradingCompany, value => item.GradingCompany = value);
        SetIfBlank(nameof(InventoryItem.Grade), item.Grade, source.Grade, value => item.Grade = value);

        if (!item.Rookie && source.Rookie)
        {
            item.Rookie = true;
            applied.Add(nameof(InventoryItem.Rookie));
        }

        if (!item.Autograph && source.Autograph)
        {
            item.Autograph = true;
            applied.Add(nameof(InventoryItem.Autograph));
        }

        if (!item.Relic && source.Relic)
        {
            item.Relic = true;
            applied.Add(nameof(InventoryItem.Relic));
        }

        if (string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(source.Name))
        {
            item.Name = source.Name;
            applied.Add(nameof(InventoryItem.Name));
        }

        if (applied.Count > 0)
        {
            item.ModifiedUtc = DateTimeOffset.UtcNow;
        }

        return applied;

        void SetIfBlank(string name, string current, string candidate, Action<string> setter)
        {
            if (!string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            setter(candidate);
            applied.Add(name);
        }
    }
}

