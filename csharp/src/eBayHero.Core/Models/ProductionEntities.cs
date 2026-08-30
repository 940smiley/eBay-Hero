namespace eBayHero.Core.Models;

public sealed class OcrImageArtifact
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string PhotoId { get; set; } = string.Empty;
    public Photo? Photo { get; set; }
    public string OcrRunId { get; set; } = string.Empty;
    public OcrRun? OcrRun { get; set; }
    public OcrImageKind Kind { get; set; } = OcrImageKind.Original;
    public string OriginalImagePath { get; set; } = string.Empty;
    public string DerivedImagePath { get; set; } = string.Empty;
    public string CropRectangleJson { get; set; } = string.Empty;
    public double RotationDegrees { get; set; }
    public string TransformMatrixJson { get; set; } = string.Empty;
    public string PreprocessingProfile { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Error { get; set; } = string.Empty;
}

public sealed class OcrReview
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string PhotoId { get; set; } = string.Empty;
    public Photo? Photo { get; set; }
    public string OcrRunId { get; set; } = string.Empty;
    public OcrRun? OcrRun { get; set; }
    public string SelectedCandidateId { get; set; } = string.Empty;
    public OcrReviewStatus Status { get; set; } = OcrReviewStatus.Pending;
    public string CorrectedText { get; set; } = string.Empty;
    public string CorrectedMetadataJson { get; set; } = "{}";
    public bool Learned { get; set; }
    public DateTimeOffset ReviewedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ImageEditSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string PhotoId { get; set; } = string.Empty;
    public Photo? Photo { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string CurrentDerivedPath { get; set; } = string.Empty;
    public bool OverwriteOriginalApproved { get; set; }
    public int CurrentOperationIndex { get; set; } = -1;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ImageEditOperation> Operations { get; } = new List<ImageEditOperation>();
}

public sealed class ImageEditOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string ImageEditSessionId { get; set; } = string.Empty;
    public ImageEditSession? ImageEditSession { get; set; }
    public ImageEditOperationKind Kind { get; set; }
    public int Sequence { get; set; }
    public string ParametersJson { get; set; } = "{}";
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string UndoPath { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PriceEvidence
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string InventoryItemId { get; set; } = string.Empty;
    public InventoryItem? InventoryItem { get; set; }
    public PricingEvidenceKind Kind { get; set; }
    public decimal Amount { get; set; }
    public decimal Shipping { get; set; }
    public decimal Fees { get; set; }
    public string Currency { get; set; } = "USD";
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTimeOffset ObservedUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool Included { get; set; } = true;
    public bool IsOutlier { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class PriceSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string InventoryItemId { get; set; } = string.Empty;
    public InventoryItem? InventoryItem { get; set; }
    public decimal Low { get; set; }
    public decimal Median { get; set; }
    public decimal Average { get; set; }
    public decimal TrimmedAverage { get; set; }
    public decimal High { get; set; }
    public decimal QuickSalePrice { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal PremiumPrice { get; set; }
    public decimal AuctionStart { get; set; }
    public decimal EstimatedFees { get; set; }
    public decimal Shipping { get; set; }
    public decimal EstimatedNet { get; set; }
    public int ComparableCount { get; set; }
    public PricingConfidence Confidence { get; set; } = PricingConfidence.Unknown;
    public bool PriceLocked { get; set; }
    public bool NeedsResearch { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string ProvenanceJson { get; set; } = "{}";
}

public sealed class SaleLot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Name { get; set; } = string.Empty;
    public LotStatus Status { get; set; } = LotStatus.Draft;
    public string Rationale { get; set; } = string.Empty;
    public decimal EstimatedIndividualValue { get; set; }
    public decimal SuggestedLotValue { get; set; }
    public decimal SuggestedAuctionStart { get; set; }
    public decimal SuggestedBuyItNow { get; set; }
    public decimal WeightOunces { get; set; }
    public decimal EstimatedFees { get; set; }
    public decimal Shipping { get; set; }
    public decimal EstimatedNet { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<SaleLotItem> Items { get; } = new List<SaleLotItem>();
}

public sealed class SaleLotItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string SaleLotId { get; set; } = string.Empty;
    public SaleLot? SaleLot { get; set; }
    public string InventoryItemId { get; set; } = string.Empty;
    public InventoryItem? InventoryItem { get; set; }
    public int SortOrder { get; set; }
    public bool ExplicitlyAllowedInMultipleActiveLots { get; set; }
}

public sealed class MarketplaceListing
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public MarketplaceListingKind Kind { get; set; } = MarketplaceListingKind.SingleCard;
    public MarketplaceListingStatus Status { get; set; } = MarketplaceListingStatus.Draft;
    public string Marketplace { get; set; } = "eBay";
    public string? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public string? SaleLotId { get; set; }
    public SaleLot? SaleLot { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAuction { get; set; }
    public bool BuyItNowEnabled { get; set; } = true;
    public bool BestOfferEnabled { get; set; }
    public int Quantity { get; set; } = 1;
    public string PaymentPolicyId { get; set; } = string.Empty;
    public string FulfillmentPolicyId { get; set; } = string.Empty;
    public string ReturnPolicyId { get; set; } = string.Empty;
    public int HandlingTimeDays { get; set; } = 3;
    public string ItemSpecificsJson { get; set; } = "{}";
    public DateTimeOffset? ScheduledUtc { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ExternalListingId { get; set; } = string.Empty;
    public string ExternalUrl { get; set; } = string.Empty;
    public bool PublishingEnabled { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<MarketplaceListingPhoto> Photos { get; } = new List<MarketplaceListingPhoto>();
    public ICollection<ListingAuditFinding> AuditFindings { get; } = new List<ListingAuditFinding>();
}

public sealed class MarketplaceListingPhoto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string MarketplaceListingId { get; set; } = string.Empty;
    public MarketplaceListing? MarketplaceListing { get; set; }
    public string PhotoId { get; set; } = string.Empty;
    public Photo? Photo { get; set; }
    public int SortOrder { get; set; }
    public PhotoViewType ViewType { get; set; } = PhotoViewType.Unknown;
}

public sealed class ListingAuditFinding
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string MarketplaceListingId { get; set; } = string.Empty;
    public MarketplaceListing? MarketplaceListing { get; set; }
    public ListingAuditSeverity Severity { get; set; } = ListingAuditSeverity.Warning;
    public AuditFixSafety FixSafety { get; set; } = AuditFixSafety.ManualOnly;
    public string RuleId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public bool Approved { get; set; }
    public bool Resolved { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EbayConnectionProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public MarketplaceEnvironment Environment { get; set; } = MarketplaceEnvironment.Sandbox;
    public string MarketplaceId { get; set; } = "EBAY_US";
    public string ClientIdHint { get; set; } = string.Empty;
    public string RuName { get; set; } = string.Empty;
    public string ScopeList { get; set; } = string.Empty;
    public EbayConnectionStatus Status { get; set; } = EbayConnectionStatus.NotConfigured;
    public string ConnectedAccountDisplay { get; set; } = string.Empty;
    public DateTimeOffset? TokenExpiresUtc { get; set; }
    public DateTimeOffset? RefreshTokenExpiresUtc { get; set; }
    public bool PublishingEnabled { get; set; }
    public string CapabilitiesJson { get; set; } = "{}";
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class JobAttempt
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string JobId { get; set; } = string.Empty;
    public Job? Job { get; set; }
    public int AttemptNumber { get; set; }
    public DateTimeOffset StartedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedUtc { get; set; }
    public string ResultJson { get; set; } = "{}";
    public string ErrorDetails { get; set; } = string.Empty;
}

