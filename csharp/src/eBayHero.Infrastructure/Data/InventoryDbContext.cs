using eBayHero.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace eBayHero.Infrastructure.Data;

/// <summary>
/// The single EF Core (SQLite) persistence context for eBay Hero.
///
/// Architectural notes:
/// - This context lives in Infrastructure so the platform-neutral Core project never
///   takes a dependency on a specific database provider.
/// - Table names intentionally match the <c>DbSet</c> property names (EF Core's default)
///   because the production diagnostics bundle and migration verification scripts assert
///   on those exact names.
/// - DateTimeOffset columns use the SQLite provider's default ISO-8601 TEXT mapping.
///   That mapping is lexicographically sortable for a fixed UTC offset, which is what
///   allows <c>OrderBy(photo =&gt; photo.ImportedUtc)</c> to translate to SQL.
/// </summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    // --- Core inventory graph -------------------------------------------------
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<PhotoItemLink> PhotoItemLinks => Set<PhotoItemLink>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PhotoTag> PhotoTags => Set<PhotoTag>();
    public DbSet<ItemTag> ItemTags => Set<ItemTag>();
    public DbSet<SourceRoot> SourceRoots => Set<SourceRoot>();
    public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();

    // --- OCR ---------------------------------------------------------------
    public DbSet<OcrRun> OcrRuns => Set<OcrRun>();
    public DbSet<OcrCandidate> OcrCandidates => Set<OcrCandidate>();
    public DbSet<OcrCorrection> OcrCorrections => Set<OcrCorrection>();
    public DbSet<OcrVocabulary> OcrVocabularies => Set<OcrVocabulary>();
    public DbSet<OcrImageArtifact> OcrImageArtifacts => Set<OcrImageArtifact>();
    public DbSet<OcrReview> OcrReviews => Set<OcrReview>();

    // --- Image editing -----------------------------------------------------
    public DbSet<ImageEditSession> ImageEditSessions => Set<ImageEditSession>();
    public DbSet<ImageEditOperation> ImageEditOperations => Set<ImageEditOperation>();

    // --- Pricing / lots / listings ----------------------------------------
    public DbSet<PriceEvidence> PriceEvidence => Set<PriceEvidence>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<SaleLot> SaleLots => Set<SaleLot>();
    public DbSet<SaleLotItem> SaleLotItems => Set<SaleLotItem>();
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<MarketplaceListingPhoto> MarketplaceListingPhotos => Set<MarketplaceListingPhoto>();
    public DbSet<ListingAuditFinding> ListingAuditFindings => Set<ListingAuditFinding>();
    public DbSet<EbayConnectionProfile> EbayConnectionProfiles => Set<EbayConnectionProfile>();

    // --- Exports / operations / diagnostics -------------------------------
    public DbSet<ExportRun> ExportRuns => Set<ExportRun>();
    public DbSet<ExportItem> ExportItems => Set<ExportItem>();
    public DbSet<FileOperation> FileOperations => Set<FileOperation>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobAttempt> JobAttempts => Set<JobAttempt>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();

    /// <summary>
    /// Applies cross-cutting column conventions. Every DateTimeOffset (and its nullable
    /// form) is stored as UTC ticks; see <see cref=\"UtcTicksConverter\"/> for why.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Core inventory graph -------------------------------------------
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(photo => photo.Id);
            entity.Property(photo => photo.Id).HasMaxLength(64);
            entity.HasIndex(photo => photo.FullPath);
            entity.HasIndex(photo => photo.Sha256);
            entity.HasIndex(photo => photo.ImportedUtc);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(64);
            entity.HasIndex(item => item.Name);
            entity.HasIndex(item => item.ListingStatus);
            entity.HasIndex(item => item.DateListedUtc);
        });

        // Many-to-many join between photos and items. Composite key prevents duplicate
        // pairings; SortOrder drives eBay photo ordering.
        modelBuilder.Entity<PhotoItemLink>(entity =>
        {
            entity.HasKey(link => new { link.PhotoId, link.InventoryItemId });
            entity.HasOne(link => link.Photo)
                .WithMany(photo => photo.ItemLinks)
                .HasForeignKey(link => link.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.InventoryItem)
                .WithMany(item => item.PhotoLinks)
                .HasForeignKey(link => link.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(link => link.InventoryItemId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(tag => tag.Id);
            entity.Property(tag => tag.Id).HasMaxLength(64);
            entity.Property(tag => tag.Name).IsRequired();
            entity.HasIndex(tag => tag.Name).IsUnique();
        });

        modelBuilder.Entity<PhotoTag>(entity =>
        {
            entity.HasKey(link => new { link.PhotoId, link.TagId });
            entity.HasOne(link => link.Photo)
                .WithMany(photo => photo.PhotoTags)
                .HasForeignKey(link => link.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Tag)
                .WithMany(tag => tag.PhotoTags)
                .HasForeignKey(link => link.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemTag>(entity =>
        {
            entity.HasKey(link => new { link.InventoryItemId, link.TagId });
            entity.HasOne(link => link.InventoryItem)
                .WithMany(item => item.ItemTags)
                .HasForeignKey(link => link.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Tag)
                .WithMany(tag => tag.ItemTags)
                .HasForeignKey(link => link.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SourceRoot>(entity =>
        {
            entity.HasKey(root => root.Id);
            entity.Property(root => root.Id).HasMaxLength(64);
            entity.HasIndex(root => root.Path);
        });

        modelBuilder.Entity<CustomFieldValue>(entity =>
        {
            entity.HasKey(value => value.Id);
            entity.HasOne(value => value.InventoryItem)
                .WithMany(item => item.CustomFields)
                .HasForeignKey(value => value.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(value => new { value.InventoryItemId, value.FieldName }).IsUnique();
        });

        // --- OCR -------------------------------------------------------------
        modelBuilder.Entity<OcrRun>(entity =>
        {
            entity.HasKey(run => run.Id);
            entity.HasOne(run => run.Photo)
                .WithMany()
                .HasForeignKey(run => run.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(run => run.PhotoId);
        });

        modelBuilder.Entity<OcrCandidate>(entity =>
        {
            entity.HasKey(candidate => candidate.Id);
            entity.HasOne(candidate => candidate.OcrRun)
                .WithMany(run => run.Candidates)
                .HasForeignKey(candidate => candidate.OcrRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OcrCorrection>(entity =>
        {
            entity.HasKey(correction => correction.Id);
            entity.HasIndex(correction => new { correction.Field, correction.IncorrectText });
        });

        modelBuilder.Entity<OcrVocabulary>(entity =>
        {
            entity.HasKey(word => word.Id);
            entity.HasIndex(word => word.Word).IsUnique();
        });

        modelBuilder.Entity<OcrImageArtifact>(entity =>
        {
            entity.HasKey(artifact => artifact.Id);
            entity.HasOne(artifact => artifact.Photo)
                .WithMany()
                .HasForeignKey(artifact => artifact.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(artifact => artifact.PhotoId);
        });

        modelBuilder.Entity<OcrReview>(entity =>
        {
            entity.HasKey(review => review.Id);
            entity.HasOne(review => review.Photo)
                .WithMany()
                .HasForeignKey(review => review.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Image editing ---------------------------------------------------
        modelBuilder.Entity<ImageEditSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.HasOne(session => session.Photo)
                .WithMany()
                .HasForeignKey(session => session.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImageEditOperation>(entity =>
        {
            entity.HasKey(operation => operation.Id);
            entity.HasOne(operation => operation.ImageEditSession)
                .WithMany(session => session.Operations)
                .HasForeignKey(operation => operation.ImageEditSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Pricing / lots / listings --------------------------------------
        modelBuilder.Entity<PriceEvidence>(entity =>
        {
            entity.HasKey(evidence => evidence.Id);
            entity.HasOne(evidence => evidence.InventoryItem)
                .WithMany()
                .HasForeignKey(evidence => evidence.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(evidence => evidence.InventoryItemId);
        });

        modelBuilder.Entity<PriceSnapshot>(entity =>
        {
            entity.HasKey(snapshot => snapshot.Id);
            entity.HasOne(snapshot => snapshot.InventoryItem)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(snapshot => snapshot.InventoryItemId);
        });

        modelBuilder.Entity<SaleLot>(entity => entity.HasKey(lot => lot.Id));

        modelBuilder.Entity<SaleLotItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasOne(item => item.SaleLot)
                .WithMany(lot => lot.Items)
                .HasForeignKey(item => item.SaleLotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.InventoryItem)
                .WithMany()
                .HasForeignKey(item => item.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketplaceListing>(entity =>
        {
            entity.HasKey(listing => listing.Id);
            entity.HasOne(listing => listing.InventoryItem)
                .WithMany()
                .HasForeignKey(listing => listing.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(listing => listing.SaleLot)
                .WithMany()
                .HasForeignKey(listing => listing.SaleLotId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(listing => listing.Status);
            entity.HasIndex(listing => listing.ModifiedUtc);
        });

        modelBuilder.Entity<MarketplaceListingPhoto>(entity =>
        {
            entity.HasKey(photo => photo.Id);
            entity.HasOne(photo => photo.MarketplaceListing)
                .WithMany(listing => listing.Photos)
                .HasForeignKey(photo => photo.MarketplaceListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ListingAuditFinding>(entity =>
        {
            entity.HasKey(finding => finding.Id);
            entity.HasOne(finding => finding.MarketplaceListing)
                .WithMany(listing => listing.AuditFindings)
                .HasForeignKey(finding => finding.MarketplaceListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EbayConnectionProfile>(entity =>
        {
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.Environment);
        });

        // --- Exports / operations / diagnostics ------------------------------
        modelBuilder.Entity<ExportRun>(entity => entity.HasKey(run => run.Id));

        modelBuilder.Entity<ExportItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasOne(item => item.ExportRun)
                .WithMany(run => run.Items)
                .HasForeignKey(item => item.ExportRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FileOperation>(entity =>
        {
            entity.HasKey(operation => operation.Id);
            entity.HasIndex(operation => operation.Status);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(job => job.Id);
            entity.HasIndex(job => job.Status);
            entity.HasIndex(job => job.Kind);
        });

        modelBuilder.Entity<JobAttempt>(entity =>
        {
            entity.HasKey(attempt => attempt.Id);
            entity.HasOne(attempt => attempt.Job)
                .WithMany()
                .HasForeignKey(attempt => attempt.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(setting => setting.Key);
            entity.Property(setting => setting.Key).HasMaxLength(200);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(audit => audit.Id);
            entity.HasIndex(audit => audit.CreatedUtc);
        });

        modelBuilder.Entity<SavedView>(entity =>
        {
            entity.HasKey(view => view.Id);
            entity.HasIndex(view => view.Name);
        });
    }
}