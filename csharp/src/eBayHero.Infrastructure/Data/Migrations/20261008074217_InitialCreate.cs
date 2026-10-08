using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBayHero.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    SourcePath = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationPath = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EbayConnectionProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Environment = table.Column<int>(type: "INTEGER", nullable: false),
                    MarketplaceId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientIdHint = table.Column<string>(type: "TEXT", nullable: false),
                    RuName = table.Column<string>(type: "TEXT", nullable: false),
                    ScopeList = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectedAccountDisplay = table.Column<string>(type: "TEXT", nullable: false),
                    TokenExpiresUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    RefreshTokenExpiresUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    PublishingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CapabilitiesJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbayConnectionProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ExportRoot = table.Column<string>(type: "TEXT", nullable: false),
                    ManifestCsvPath = table.Column<string>(type: "TEXT", nullable: false),
                    ManifestJsonPath = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PhotoCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileOperations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SourcePath = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationPath = table.Column<string>(type: "TEXT", nullable: false),
                    ReversalSourcePath = table.Column<string>(type: "TEXT", nullable: false),
                    ReversalDestinationPath = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedUtc = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    SportOrGame = table.Column<string>(type: "TEXT", nullable: false),
                    PlayerOrTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<string>(type: "TEXT", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    SetName = table.Column<string>(type: "TEXT", nullable: false),
                    CardNumber = table.Column<string>(type: "TEXT", nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ISBN = table.Column<string>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    Rookie = table.Column<bool>(type: "INTEGER", nullable: false),
                    Autograph = table.Column<bool>(type: "INTEGER", nullable: false),
                    Relic = table.Column<bool>(type: "INTEGER", nullable: false),
                    Team = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false),
                    GradingCompany = table.Column<string>(type: "TEXT", nullable: false),
                    Grade = table.Column<string>(type: "TEXT", nullable: false),
                    ListingStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ListingPlatform = table.Column<string>(type: "TEXT", nullable: false),
                    ListingId = table.Column<string>(type: "TEXT", nullable: false),
                    ListingUrl = table.Column<string>(type: "TEXT", nullable: false),
                    DateListedUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    DateSoldUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    SalePrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Progress = table.Column<double>(type: "REAL", nullable: false),
                    CurrentFile = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    StartedUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    EndedUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    ErrorDetails = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OcrCorrections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    IncorrectText = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectedText = table.Column<string>(type: "TEXT", nullable: false),
                    Field = table.Column<string>(type: "TEXT", nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUsedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrCorrections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OcrVocabularies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Word = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrVocabularies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FullPath = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalPath = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    Extension = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ImportedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    PerceptualHash = table.Column<ulong>(type: "INTEGER", nullable: true),
                    IsMissing = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDuplicate = table.Column<bool>(type: "INTEGER", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "TEXT", nullable: false),
                    OcrText = table.Column<string>(type: "TEXT", nullable: false),
                    OcrConfidence = table.Column<double>(type: "REAL", nullable: false),
                    OcrProfile = table.Column<int>(type: "INTEGER", nullable: false),
                    OcrReviewed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ViewType = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaleLots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedIndividualValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    SuggestedLotValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    SuggestedAuctionStart = table.Column<decimal>(type: "TEXT", nullable: false),
                    SuggestedBuyItNow = table.Column<decimal>(type: "TEXT", nullable: false),
                    WeightOunces = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstimatedFees = table.Column<decimal>(type: "TEXT", nullable: false),
                    Shipping = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstimatedNet = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleLots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FilterJson = table.Column<string>(type: "TEXT", nullable: false),
                    ColumnLayoutJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceRoots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IndexInPlace = table.Column<bool>(type: "INTEGER", nullable: false),
                    CopyIntoManagedStorage = table.Column<bool>(type: "INTEGER", nullable: false),
                    MoveIntoManagedStorage = table.Column<bool>(type: "INTEGER", nullable: false),
                    Recursive = table.Column<bool>(type: "INTEGER", nullable: false),
                    WatchForChanges = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludePatterns = table.Column<string>(type: "TEXT", nullable: false),
                    ExcludePatterns = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    LastScanUtc = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRoots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ExportRunId = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    PhotoOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    SourcePath = table.Column<string>(type: "TEXT", nullable: false),
                    ExportPath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportItems_ExportRuns_ExportRunId",
                        column: x => x.ExportRunId,
                        principalTable: "ExportRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", nullable: false),
                    FieldValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldValues_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceEvidence",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Shipping = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fees = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    ObservedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Included = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOutlier = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceEvidence_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    Median = table.Column<decimal>(type: "TEXT", nullable: false),
                    Average = table.Column<decimal>(type: "TEXT", nullable: false),
                    TrimmedAverage = table.Column<decimal>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuickSalePrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MarketPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    PremiumPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    AuctionStart = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstimatedFees = table.Column<decimal>(type: "TEXT", nullable: false),
                    Shipping = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstimatedNet = table.Column<decimal>(type: "TEXT", nullable: false),
                    ComparableCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<int>(type: "INTEGER", nullable: false),
                    PriceLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    NeedsResearch = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ProvenanceJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceSnapshots_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobAttempts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    EndedUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    ResultJson = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorDetails = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobAttempts_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageEditSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    SourcePath = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentDerivedPath = table.Column<string>(type: "TEXT", nullable: false),
                    OverwriteOriginalApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentOperationIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageEditSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageEditSessions_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OcrRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    Profile = table.Column<int>(type: "INTEGER", nullable: false),
                    BestText = table.Column<string>(type: "TEXT", nullable: false),
                    AverageConfidence = table.Column<double>(type: "REAL", nullable: false),
                    StartedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ExecutionMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorOutput = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcrRuns_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoItemLinks",
                columns: table => new
                {
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageRoleCode = table.Column<string>(type: "TEXT", nullable: false),
                    ViewType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoItemLinks", x => new { x.PhotoId, x.InventoryItemId });
                    table.ForeignKey(
                        name: "FK_PhotoItemLinks_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoItemLinks_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceListings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Marketplace = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: true),
                    SaleLotId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionDescription = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsAuction = table.Column<bool>(type: "INTEGER", nullable: false),
                    BuyItNowEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    BestOfferEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentPolicyId = table.Column<string>(type: "TEXT", nullable: false),
                    FulfillmentPolicyId = table.Column<string>(type: "TEXT", nullable: false),
                    ReturnPolicyId = table.Column<string>(type: "TEXT", nullable: false),
                    HandlingTimeDays = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemSpecificsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduledUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalListingId = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalUrl = table.Column<string>(type: "TEXT", nullable: false),
                    PublishingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceListings_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MarketplaceListings_SaleLots_SaleLotId",
                        column: x => x.SaleLotId,
                        principalTable: "SaleLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SaleLotItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SaleLotId = table.Column<string>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ExplicitlyAllowedInMultipleActiveLots = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleLotItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleLotItems_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleLotItems_SaleLots_SaleLotId",
                        column: x => x.SaleLotId,
                        principalTable: "SaleLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemTags",
                columns: table => new
                {
                    InventoryItemId = table.Column<string>(type: "TEXT", nullable: false),
                    TagId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTags", x => new { x.InventoryItemId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ItemTags_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoTags",
                columns: table => new
                {
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    TagId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoTags", x => new { x.PhotoId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PhotoTags_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageEditOperations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ImageEditSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    ParametersJson = table.Column<string>(type: "TEXT", nullable: false),
                    InputPath = table.Column<string>(type: "TEXT", nullable: false),
                    OutputPath = table.Column<string>(type: "TEXT", nullable: false),
                    UndoPath = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageEditOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageEditOperations_ImageEditSessions_ImageEditSessionId",
                        column: x => x.ImageEditSessionId,
                        principalTable: "ImageEditSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OcrCandidates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OcrRunId = table.Column<string>(type: "TEXT", nullable: false),
                    RecognizedText = table.Column<string>(type: "TEXT", nullable: false),
                    RawTsv = table.Column<string>(type: "TEXT", nullable: false),
                    WordConfidence = table.Column<double>(type: "REAL", nullable: false),
                    AverageConfidence = table.Column<double>(type: "REAL", nullable: false),
                    Crop = table.Column<string>(type: "TEXT", nullable: false),
                    PreprocessingProfile = table.Column<string>(type: "TEXT", nullable: false),
                    PageSegmentationMode = table.Column<int>(type: "INTEGER", nullable: false),
                    CandidateScore = table.Column<double>(type: "REAL", nullable: false),
                    ExecutionMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorOutput = table.Column<string>(type: "TEXT", nullable: false),
                    DerivedImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    CropRectangleJson = table.Column<string>(type: "TEXT", nullable: false),
                    RotationDegrees = table.Column<double>(type: "REAL", nullable: false),
                    TransformMatrixJson = table.Column<string>(type: "TEXT", nullable: false),
                    WordBoxesJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcrCandidates_OcrRuns_OcrRunId",
                        column: x => x.OcrRunId,
                        principalTable: "OcrRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OcrImageArtifacts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    OcrRunId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    DerivedImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    CropRectangleJson = table.Column<string>(type: "TEXT", nullable: false),
                    RotationDegrees = table.Column<double>(type: "REAL", nullable: false),
                    TransformMatrixJson = table.Column<string>(type: "TEXT", nullable: false),
                    PreprocessingProfile = table.Column<string>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrImageArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcrImageArtifacts_OcrRuns_OcrRunId",
                        column: x => x.OcrRunId,
                        principalTable: "OcrRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OcrImageArtifacts_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OcrReviews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    OcrRunId = table.Column<string>(type: "TEXT", nullable: false),
                    SelectedCandidateId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectedText = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectedMetadataJson = table.Column<string>(type: "TEXT", nullable: false),
                    Learned = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReviewedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcrReviews_OcrRuns_OcrRunId",
                        column: x => x.OcrRunId,
                        principalTable: "OcrRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OcrReviews_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListingAuditFindings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceListingId = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    FixSafety = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleId = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: false),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: false),
                    Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                    Resolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingAuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingAuditFindings_MarketplaceListings_MarketplaceListingId",
                        column: x => x.MarketplaceListingId,
                        principalTable: "MarketplaceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceListingPhotos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceListingId = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceListingPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceListingPhotos_MarketplaceListings_MarketplaceListingId",
                        column: x => x.MarketplaceListingId,
                        principalTable: "MarketplaceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketplaceListingPhotos_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CreatedUtc",
                table: "AuditEvents",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_InventoryItemId_FieldName",
                table: "CustomFieldValues",
                columns: new[] { "InventoryItemId", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EbayConnectionProfiles_Environment",
                table: "EbayConnectionProfiles",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_ExportItems_ExportRunId",
                table: "ExportItems",
                column: "ExportRunId");

            migrationBuilder.CreateIndex(
                name: "IX_FileOperations_Status",
                table: "FileOperations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImageEditOperations_ImageEditSessionId",
                table: "ImageEditOperations",
                column: "ImageEditSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageEditSessions_PhotoId",
                table: "ImageEditSessions",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_DateListedUtc",
                table: "InventoryItems",
                column: "DateListedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ListingStatus",
                table: "InventoryItems",
                column: "ListingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Name",
                table: "InventoryItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTags_TagId",
                table: "ItemTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAttempts_JobId",
                table: "JobAttempts",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Kind",
                table: "Jobs",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status",
                table: "Jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ListingAuditFindings_MarketplaceListingId",
                table: "ListingAuditFindings",
                column: "MarketplaceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListingPhotos_MarketplaceListingId",
                table: "MarketplaceListingPhotos",
                column: "MarketplaceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListingPhotos_PhotoId",
                table: "MarketplaceListingPhotos",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListings_InventoryItemId",
                table: "MarketplaceListings",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListings_ModifiedUtc",
                table: "MarketplaceListings",
                column: "ModifiedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListings_SaleLotId",
                table: "MarketplaceListings",
                column: "SaleLotId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListings_Status",
                table: "MarketplaceListings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OcrCandidates_OcrRunId",
                table: "OcrCandidates",
                column: "OcrRunId");

            migrationBuilder.CreateIndex(
                name: "IX_OcrCorrections_Field_IncorrectText",
                table: "OcrCorrections",
                columns: new[] { "Field", "IncorrectText" });

            migrationBuilder.CreateIndex(
                name: "IX_OcrImageArtifacts_OcrRunId",
                table: "OcrImageArtifacts",
                column: "OcrRunId");

            migrationBuilder.CreateIndex(
                name: "IX_OcrImageArtifacts_PhotoId",
                table: "OcrImageArtifacts",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_OcrReviews_OcrRunId",
                table: "OcrReviews",
                column: "OcrRunId");

            migrationBuilder.CreateIndex(
                name: "IX_OcrReviews_PhotoId",
                table: "OcrReviews",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_OcrRuns_PhotoId",
                table: "OcrRuns",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_OcrVocabularies_Word",
                table: "OcrVocabularies",
                column: "Word",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoItemLinks_InventoryItemId",
                table: "PhotoItemLinks",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TagId",
                table: "PhotoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_FullPath",
                table: "Photos",
                column: "FullPath");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ImportedUtc",
                table: "Photos",
                column: "ImportedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Sha256",
                table: "Photos",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_PriceEvidence_InventoryItemId",
                table: "PriceEvidence",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_InventoryItemId",
                table: "PriceSnapshots",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLotItems_InventoryItemId",
                table: "SaleLotItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLotItems_SaleLotId",
                table: "SaleLotItems",
                column: "SaleLotId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_Name",
                table: "SavedViews",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRoots_Path",
                table: "SourceRoots",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "CustomFieldValues");

            migrationBuilder.DropTable(
                name: "EbayConnectionProfiles");

            migrationBuilder.DropTable(
                name: "ExportItems");

            migrationBuilder.DropTable(
                name: "FileOperations");

            migrationBuilder.DropTable(
                name: "ImageEditOperations");

            migrationBuilder.DropTable(
                name: "ItemTags");

            migrationBuilder.DropTable(
                name: "JobAttempts");

            migrationBuilder.DropTable(
                name: "ListingAuditFindings");

            migrationBuilder.DropTable(
                name: "MarketplaceListingPhotos");

            migrationBuilder.DropTable(
                name: "OcrCandidates");

            migrationBuilder.DropTable(
                name: "OcrCorrections");

            migrationBuilder.DropTable(
                name: "OcrImageArtifacts");

            migrationBuilder.DropTable(
                name: "OcrReviews");

            migrationBuilder.DropTable(
                name: "OcrVocabularies");

            migrationBuilder.DropTable(
                name: "PhotoItemLinks");

            migrationBuilder.DropTable(
                name: "PhotoTags");

            migrationBuilder.DropTable(
                name: "PriceEvidence");

            migrationBuilder.DropTable(
                name: "PriceSnapshots");

            migrationBuilder.DropTable(
                name: "SaleLotItems");

            migrationBuilder.DropTable(
                name: "SavedViews");

            migrationBuilder.DropTable(
                name: "SourceRoots");

            migrationBuilder.DropTable(
                name: "ExportRuns");

            migrationBuilder.DropTable(
                name: "ImageEditSessions");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "MarketplaceListings");

            migrationBuilder.DropTable(
                name: "OcrRuns");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "SaleLots");

            migrationBuilder.DropTable(
                name: "Photos");
        }
    }
}
