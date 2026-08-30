using eBayHero.Core.Configuration;
using eBayHero.Core.Models;

namespace eBayHero.Core.Services;

public sealed record ScanPhotoResult(
    string FullPath,
    string NormalizedPath,
    long FileSize,
    DateTimeOffset ModifiedUtc,
    string Extension);

public sealed record ImportPlanItem(
    string SourcePath,
    string DestinationPath,
    FileOperationKind OperationKind,
    bool AlreadyIndexed,
    bool DuplicateContent,
    string Sha256);

public sealed record FileOperationPlan(
    IReadOnlyList<ImportPlanItem> Items,
    int CollisionCount,
    int DuplicateCount,
    bool TouchesLiveInventory);

public sealed record OcrRequest(
    string PhotoId,
    string ImagePath,
    OcrProfile Profile,
    string Language,
    string TesseractPath,
    string UserWordsPath,
    string WorkingRoot = "",
    string CustomCropJson = "");

public sealed record OcrCandidateResult(
    string Text,
    string RawTsv,
    double AverageConfidence,
    string Crop,
    string PreprocessingProfile,
    int PageSegmentationMode,
    double Score,
    int ExecutionMilliseconds,
    string ErrorOutput,
    string DerivedImagePath = "",
    string CropRectangleJson = "",
    double RotationDegrees = 0,
    string TransformMatrixJson = "",
    string WordBoxesJson = "[]");

public sealed record OcrRunResult(
    string BestText,
    double AverageConfidence,
    IReadOnlyList<OcrCandidateResult> Candidates,
    string ErrorOutput);

public sealed record MigrationOptions(
    string JsonPath,
    string DatabasePath,
    bool Apply,
    bool AllowLiveInventoryAccess);

public sealed record MigrationMissingFile(
    string PhotoId,
    string FullPath,
    string GroupId,
    string FileName,
    IReadOnlyList<string> CandidatePaths);

public sealed record MigrationReport(
    bool Applied,
    int PhotosRead,
    int PhotosInserted,
    int PhotosUpdated,
    int ItemsRead,
    int ItemsInserted,
    int LinksInserted,
    int TagsInserted,
    int MissingFiles,
    string BackupPath,
    string ReportPath,
    IReadOnlyList<string> Warnings)
{
    public IReadOnlyList<MigrationMissingFile> MissingFileDetails { get; init; } = [];
}

public sealed record CardOpsImportOptions(
    string SourceDatabasePath,
    string TargetDatabasePath,
    bool Apply,
    bool AllowLiveInventoryAccess);

public sealed record CardOpsImportReport(
    bool Applied,
    int CardsRead,
    int CardsInserted,
    int CardsUpdated,
    int ImagesRead,
    int ImagesInserted,
    int ImagesUpdated,
    int LinksInserted,
    int SourceRootsRead,
    int SourceRootsInserted,
    int TagsInserted,
    int CustomFieldsInserted,
    string BackupPath,
    IReadOnlyList<string> Warnings);

public sealed record ExportRequest(
    string ExportRoot,
    IReadOnlyList<InventoryItem> Items,
    IReadOnlyDictionary<string, IReadOnlyList<Photo>> PhotosByItemId,
    ListingStatus? OfferedStatusChange);

public sealed record ExportResult(
    string ExportDirectory,
    string CsvManifestPath,
    string JsonManifestPath,
    string ReadmePath,
    int ItemCount,
    int PhotoCount);

public sealed record DiagnosticsBundleRequest(
    string OperationsRoot,
    int MaxAuditEvents = 100,
    int MaxFailedJobs = 50,
    int MaxLogFiles = 3,
    int MaxLogTailLines = 400,
    long MaxLogFileBytes = 1_048_576);

public sealed record DiagnosticsBundleResult(
    string BundleDirectory,
    string ZipPath,
    int SectionCount,
    IReadOnlyList<string> Warnings);

public sealed record ImagePreprocessRequest(
    string PhotoId,
    string SourcePath,
    OcrProfile Profile,
    string WorkingRoot,
    string CustomCropJson = "");

public sealed record ImagePreprocessVariant(
    OcrImageKind Kind,
    string ProfileName,
    string SourcePath,
    string DerivedPath,
    string CropRectangleJson,
    double RotationDegrees,
    string TransformMatrixJson,
    int Width,
    int Height);

public sealed record ImagePreprocessResult(
    IReadOnlyList<ImagePreprocessVariant> Variants,
    IReadOnlyList<string> Warnings);

public sealed record ImageEditCommand(
    ImageEditOperationKind Kind,
    string ParametersJson = "{}");

public sealed record ImageEditRequest(
    string PhotoId,
    string SourcePath,
    string WorkingRoot,
    IReadOnlyList<ImageEditCommand> Commands,
    bool OverwriteOriginal = false);

public sealed record ImageEditResult(
    string OutputPath,
    IReadOnlyList<ImageEditOperation> Operations,
    bool OriginalOverwritten);

public sealed record RootValidationResult(
    string NormalizedPath,
    RootAvailability Availability,
    bool IsDuplicateEquivalent,
    string Message);

public sealed record PriceComputationRequest(
    IReadOnlyList<PriceEvidence> Evidence,
    decimal EstimatedShipping,
    decimal FeeRate,
    bool PriceLocked = false);

public sealed record PriceComputationResult(
    decimal Low,
    decimal Median,
    decimal Average,
    decimal TrimmedAverage,
    decimal High,
    decimal QuickSalePrice,
    decimal MarketPrice,
    decimal PremiumPrice,
    decimal AuctionStart,
    decimal EstimatedFees,
    decimal Shipping,
    decimal EstimatedNet,
    int ComparableCount,
    PricingConfidence Confidence);

public sealed record LotBuildRequest(
    string Name,
    IReadOnlyList<InventoryItem> Items,
    IReadOnlyDictionary<string, PriceSnapshot> PricesByItemId,
    bool AllowMultipleActiveLots = false);

public sealed record LotBuildResult(
    SaleLot Lot,
    IReadOnlyList<string> Warnings);

public sealed record ListingDraftRequest(
    InventoryItem? Item,
    SaleLot? Lot,
    IReadOnlyList<Photo> Photos,
    PriceSnapshot? Price);

public sealed record ListingAuditResult(
    IReadOnlyList<ListingAuditFinding> Findings,
    bool BlocksPublishing);

public sealed record EbayCapability(
    string Id,
    string Name,
    bool Enabled,
    bool CredentialRequired,
    bool ExternalApprovalRequired,
    string Reason);

public sealed record EbayConnectionSettings(
    MarketplaceEnvironment Environment,
    string MarketplaceId,
    string ClientId,
    string ClientSecret,
    string RuName,
    IReadOnlyList<string> Scopes);

public sealed record EbayAuthorizationStart(
    string AuthorizationUrl,
    string State,
    DateTimeOffset ExpiresUtc,
    IReadOnlyList<string> Scopes);

public sealed record SecretReference(
    string Scope,
    string Name);

public sealed record StorageCandidate(
    string SourcePath,
    string DestinationPath,
    long SizeBytes,
    string Classification,
    string Reason,
    bool ApplyAllowed);

public sealed record CardFieldEvidence(
    string FieldName,
    string Value,
    string SourceType,
    string SourceIdentifier,
    double Confidence);

public sealed record CardMetadataAnalysis(
    InventoryItem Candidate,
    double Confidence,
    string ProcessingStatus,
    IReadOnlyList<string> UnresolvedFields,
    IReadOnlyList<CardFieldEvidence> Evidence,
    string NormalizedText);

public sealed record ListingRecommendation(
    string Title,
    int Length,
    int Limit,
    IReadOnlyList<string> Warnings,
    string RecommendedListingFormat,
    decimal? RecommendedPrice,
    string PricingSource,
    bool PricingConfigured,
    string? PriceCaution,
    string LotAssignment,
    string DataSource);

public sealed record FrontBackPairCandidate(
    string FrontPhotoId,
    string BackPhotoId,
    double Confidence,
    string Reason);

public interface IPathService
{
    string NormalizePath(string path);
    bool IsSamePath(string left, string right);
    bool IsChildOf(string childPath, string parentPath);
}

public interface IFileScanner
{
    IAsyncEnumerable<ScanPhotoResult> ScanAsync(SourceRoot root, InventoryOptions options, CancellationToken cancellationToken);
}

public interface IHashService
{
    Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken);
}

public interface IFileOperationPlanner
{
    Task<FileOperationPlan> PlanImportAsync(
        string sourceRoot,
        string destinationRoot,
        FileOperationKind operationKind,
        bool preserveFolders,
        IReadOnlySet<string> indexedPaths,
        IReadOnlySet<string> knownHashes,
        CancellationToken cancellationToken);
}

public interface IOcrService
{
    Task<OcrRunResult> RunAsync(OcrRequest request, CancellationToken cancellationToken);
}

public interface IOcrLearningService
{
    Task SaveCorrectionAsync(string incorrectText, string correctedText, string field, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> RegenerateUserWordsAsync(IEnumerable<string> inventoryVocabulary, CancellationToken cancellationToken);
    string ApplyCorrections(string text, string field);
}

public interface IJsonMigrationService
{
    Task<MigrationReport> MigrateAsync(MigrationOptions options, CancellationToken cancellationToken);
}

public interface ICardOpsImportService
{
    Task<CardOpsImportReport> ImportAsync(CardOpsImportOptions options, CancellationToken cancellationToken);
}

public interface IEbayExportService
{
    Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken);
}

public interface IDiagnosticsBundleService
{
    Task<DiagnosticsBundleResult> CollectAsync(DiagnosticsBundleRequest request, CancellationToken cancellationToken);
}

public interface IImagePreprocessingService
{
    Task<ImagePreprocessResult> PrepareAsync(ImagePreprocessRequest request, CancellationToken cancellationToken);
}

public interface IImageEditService
{
    Task<ImageEditResult> ApplyAsync(ImageEditRequest request, CancellationToken cancellationToken);
}

public interface IRootManagementService
{
    RootValidationResult ValidateRoot(string path, IEnumerable<SourceRoot> existingRoots);
}

public interface IPricingService
{
    PriceComputationResult Compute(PriceComputationRequest request);
}

public interface ILotBuilderService
{
    LotBuildResult BuildLot(LotBuildRequest request);
}

public interface IListingDraftService
{
    MarketplaceListing CreateDraft(ListingDraftRequest request);
}

public interface IListingAuditService
{
    ListingAuditResult Audit(MarketplaceListing listing, InventoryItem? item, IReadOnlyList<Photo> photos, PriceSnapshot? price);
}

public interface ISecretStore
{
    Task SaveAsync(SecretReference reference, string secretValue, CancellationToken cancellationToken);
    Task<string?> ReadAsync(SecretReference reference, CancellationToken cancellationToken);
    Task DeleteAsync(SecretReference reference, CancellationToken cancellationToken);
}

public interface ISecretRedactor
{
    string Redact(string text);
}

public interface IEbayConnectionService
{
    IReadOnlyList<EbayCapability> GetCapabilities(EbayConnectionProfile profile);
    EbayAuthorizationStart StartAuthorization(EbayConnectionSettings settings, string callbackUri);
    bool ValidateOAuthState(string expectedState, string receivedState);
}

