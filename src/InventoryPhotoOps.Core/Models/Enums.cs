namespace InventoryPhotoOps.Core.Models;

public enum ListingStatus
{
    NotListed,
    ReadyToList,
    Drafted,
    Draft,
    Ready,
    Scheduled,
    Active,
    CurrentlyListed,
    Sold,
    Ended,
    Cancelled,
    Error,
    Listed,
    Archived
}

public enum PhotoViewType
{
    Unknown,
    Front,
    Back,
    Top,
    Bottom,
    Left,
    Right,
    FrontUpperLeft,
    FrontUpperRight,
    FrontLowerLeft,
    FrontLowerRight,
    BackUpperLeft,
    BackUpperRight,
    BackLowerLeft,
    BackLowerRight,
    Spine,
    CopyrightPage,
    SerialNumber,
    CardNumber,
    ConditionCloseup,
    Other
}

public enum FileOperationKind
{
    CopyImport,
    MoveImport,
    IndexInPlace,
    Rename,
    Move,
    Organize,
    ExportCopy,
    DeleteToRecycleBin
}

public enum FileOperationStatus
{
    Planned,
    Completed,
    Failed,
    RolledBack
}

public enum JobKind
{
    Scan,
    Import,
    Hashing,
    ThumbnailGeneration,
    Ocr,
    OcrPreprocess,
    ImageEdit,
    Grouping,
    Pricing,
    LotBuild,
    ListingAudit,
    EbayOAuth,
    EbaySync,
    EbayPublish,
    FileMove,
    Rename,
    Export,
    MissingFileCheck,
    DuplicateDetection
}

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum OcrProfile
{
    Auto,
    TradingCardFront,
    TradingCardBack,
    BookCover,
    BookCopyrightPage,
    NameplateTitle,
    CardNumber,
    SerialNumber,
    GradingLabel,
    FullImage,
    CustomRegion
}

public enum OcrReviewStatus
{
    Pending,
    Accepted,
    Corrected,
    Rejected
}

public enum OcrImageKind
{
    Original,
    Cropped,
    Deskewed,
    Thresholded,
    BoundingBoxes,
    Edited
}

public enum ImageEditOperationKind
{
    RotateLeft,
    RotateRight,
    RotateArbitrary,
    Crop,
    Straighten,
    PerspectiveCorrection,
    Reset,
    SaveDerivedCopy,
    OverwriteOriginal
}

public enum PricingEvidenceKind
{
    VerifiedOwnSale,
    AuthorizedSoldComparable,
    UserImportedSoldComparable,
    ActiveListing,
    CatalogEstimate,
    AiEstimate,
    ManualValue
}

public enum PricingConfidence
{
    Unknown,
    Low,
    Medium,
    High,
    Locked
}

public enum LotStatus
{
    Draft,
    Ready,
    SentToListings,
    Archived,
    Deleted
}

public enum MarketplaceListingStatus
{
    Draft,
    Ready,
    Scheduled,
    Active,
    Sold,
    Ended,
    Cancelled,
    Error,
    Archived
}

public enum MarketplaceListingKind
{
    SingleCard,
    Lot,
    ExistingImport
}

public enum MarketplaceEnvironment
{
    Sandbox,
    Production
}

public enum EbayConnectionStatus
{
    NotConfigured,
    CredentialsConfigured,
    AuthorizationStarted,
    Connected,
    Expired,
    Revoked,
    Error
}

public enum ListingAuditSeverity
{
    Info,
    Warning,
    Error,
    Blocker
}

public enum AuditFixSafety
{
    ManualOnly,
    SafeWithApproval,
    ExternalConfirmationRequired
}

public enum RootAvailability
{
    Unknown,
    Available,
    Unavailable,
    Disabled,
    DuplicateEquivalent
}
