namespace InventoryPhotoOps.Core.Models;

public sealed class Photo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string FullPath { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ImportedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Sha256 { get; set; } = string.Empty;
    public ulong? PerceptualHash { get; set; }
    public bool IsMissing { get; set; }
    public bool IsDuplicate { get; set; }
    public string ThumbnailPath { get; set; } = string.Empty;
    public string OcrText { get; set; } = string.Empty;
    public double OcrConfidence { get; set; }
    public OcrProfile OcrProfile { get; set; } = OcrProfile.Auto;
    public bool OcrReviewed { get; set; }
    public PhotoViewType ViewType { get; set; } = PhotoViewType.Unknown;
    public string Notes { get; set; } = string.Empty;

    public ICollection<PhotoItemLink> ItemLinks { get; } = new List<PhotoItemLink>();
    public ICollection<PhotoTag> PhotoTags { get; } = new List<PhotoTag>();
}

public sealed class InventoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SportOrGame { get; set; } = string.Empty;
    public string PlayerOrTitle { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string SetName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public bool Rookie { get; set; }
    public bool Autograph { get; set; }
    public bool Relic { get; set; }
    public string Team { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string GradingCompany { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public ListingStatus ListingStatus { get; set; } = ListingStatus.NotListed;
    public string ListingPlatform { get; set; } = string.Empty;
    public string ListingId { get; set; } = string.Empty;
    public string ListingUrl { get; set; } = string.Empty;
    public DateTimeOffset? DateListedUtc { get; set; }
    public DateTimeOffset? DateSoldUtc { get; set; }
    public decimal? SalePrice { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public ICollection<PhotoItemLink> PhotoLinks { get; } = new List<PhotoItemLink>();
    public ICollection<ItemTag> ItemTags { get; } = new List<ItemTag>();
    public ICollection<CustomFieldValue> CustomFields { get; } = new List<CustomFieldValue>();
}

public sealed class PhotoItemLink
{
    public string PhotoId { get; set; } = string.Empty;
    public Photo? Photo { get; set; }
    public string InventoryItemId { get; set; } = string.Empty;
    public InventoryItem? InventoryItem { get; set; }
    public int SortOrder { get; set; }
    public string ImageRoleCode { get; set; } = string.Empty;
    public PhotoViewType ViewType { get; set; } = PhotoViewType.Unknown;
    public bool IsPrimary { get; set; }
}

public sealed class Tag
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<PhotoTag> PhotoTags { get; } = new List<PhotoTag>();
    public ICollection<ItemTag> ItemTags { get; } = new List<ItemTag>();
}

public sealed class PhotoTag
{
    public string PhotoId { get; set; } = string.Empty;
    public Photo? Photo { get; set; }
    public string TagId { get; set; } = string.Empty;
    public Tag? Tag { get; set; }
}

public sealed class ItemTag
{
    public string InventoryItemId { get; set; } = string.Empty;
    public InventoryItem? InventoryItem { get; set; }
    public string TagId { get; set; } = string.Empty;
    public Tag? Tag { get; set; }
}

public sealed class SourceRoot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Path { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool IndexInPlace { get; set; } = true;
    public bool CopyIntoManagedStorage { get; set; }
    public bool MoveIntoManagedStorage { get; set; }
    public bool Recursive { get; set; } = true;
    public bool WatchForChanges { get; set; }
    public string IncludePatterns { get; set; } = "*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp;*.gif;*.heic;*.avif";
    public string ExcludePatterns { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastScanUtc { get; set; }
}

public sealed class OcrRun
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string PhotoId { get; set; } = string.Empty;
    public Photo? Photo { get; set; }
    public OcrProfile Profile { get; set; }
    public string BestText { get; set; } = string.Empty;
    public double AverageConfidence { get; set; }
    public DateTimeOffset StartedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedUtc { get; set; } = DateTimeOffset.UtcNow;
    public int ExecutionMilliseconds { get; set; }
    public string ErrorOutput { get; set; } = string.Empty;
    public ICollection<OcrCandidate> Candidates { get; } = new List<OcrCandidate>();
}

public sealed class OcrCandidate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string OcrRunId { get; set; } = string.Empty;
    public OcrRun? OcrRun { get; set; }
    public string RecognizedText { get; set; } = string.Empty;
    public string RawTsv { get; set; } = string.Empty;
    public double WordConfidence { get; set; }
    public double AverageConfidence { get; set; }
    public string Crop { get; set; } = string.Empty;
    public string PreprocessingProfile { get; set; } = string.Empty;
    public int PageSegmentationMode { get; set; }
    public double CandidateScore { get; set; }
    public int ExecutionMilliseconds { get; set; }
    public string ErrorOutput { get; set; } = string.Empty;
    public string DerivedImagePath { get; set; } = string.Empty;
    public string CropRectangleJson { get; set; } = string.Empty;
    public double RotationDegrees { get; set; }
    public string TransformMatrixJson { get; set; } = string.Empty;
    public string WordBoxesJson { get; set; } = "[]";
}

public sealed class OcrCorrection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string IncorrectText { get; set; } = string.Empty;
    public string CorrectedText { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public string Context { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUsedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OcrVocabulary
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Word { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ExportRun
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string ExportRoot { get; set; } = string.Empty;
    public string ManifestCsvPath { get; set; } = string.Empty;
    public string ManifestJsonPath { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public int ItemCount { get; set; }
    public int PhotoCount { get; set; }
    public ICollection<ExportItem> Items { get; } = new List<ExportItem>();
}

public sealed class ExportItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string ExportRunId { get; set; } = string.Empty;
    public ExportRun? ExportRun { get; set; }
    public string InventoryItemId { get; set; } = string.Empty;
    public string PhotoId { get; set; } = string.Empty;
    public int ItemOrder { get; set; }
    public int PhotoOrder { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string ExportPath { get; set; } = string.Empty;
}

public sealed class FileOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public FileOperationKind Kind { get; set; }
    public FileOperationStatus Status { get; set; } = FileOperationStatus.Planned;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public string ReversalSourcePath { get; set; } = string.Empty;
    public string ReversalDestinationPath { get; set; } = string.Empty;
    public string PhotoId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedUtc { get; set; }
}

public sealed class Job
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public JobKind Kind { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public double Progress { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedUtc { get; set; }
    public DateTimeOffset? EndedUtc { get; set; }
    public string ErrorDetails { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}

public sealed class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AuditEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Severity { get; set; } = "Information";
    public string Operation { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string PhotoId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Exception { get; set; } = string.Empty;
}

public sealed class SavedView
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Name { get; set; } = string.Empty;
    public string FilterJson { get; set; } = "{}";
    public string ColumnLayoutJson { get; set; } = "{}";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CustomFieldValue
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string InventoryItemId { get; set; } = string.Empty;
    public InventoryItem? InventoryItem { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
}
