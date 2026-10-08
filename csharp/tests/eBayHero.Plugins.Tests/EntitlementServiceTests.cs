using eBayHero.Core.Entitlements;

namespace eBayHero.Plugins.Tests;

public sealed class EntitlementServiceTests
{
    private sealed class TestClock
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Read() => Now;
    }

    [Fact]
    public void FreeTier_GrantsCoreFreeFeatures_ButNotProOrPluginAi()
    {
        var service = new EntitlementService(new EntitlementOptions { Tier = EntitlementTier.Free });

        Assert.True(service.HasFeature(FeatureKey.ManualListingGeneration));
        Assert.True(service.HasFeature(FeatureKey.CsvImportExport));
        Assert.False(service.HasFeature(FeatureKey.BulkApiPublishing));
        Assert.False(service.HasFeature(FeatureKey.AiCardRecognition));
        Assert.False(service.IsAddonActive("cardops"));
    }

    [Fact]
    public void ProTier_GrantsCoreProFeatures()
    {
        var service = new EntitlementService(new EntitlementOptions { Tier = EntitlementTier.Pro });

        Assert.True(service.HasFeature(FeatureKey.BulkApiPublishing));
        Assert.True(service.HasFeature(FeatureKey.MultiAccountRouting));
        Assert.True(service.HasFeature(FeatureKey.AutoRelisting));
    }

    [Fact]
    public void EnablingCardOpsAddon_UnlocksFreePluginFeatures_ButNotAi()
    {
        var service = new EntitlementService(new EntitlementOptions
        {
            Tier = EntitlementTier.Free,
            Addons = [FeatureCatalog.CardOpsAddon]
        });

        Assert.True(service.HasFeature(FeatureKey.CardInventorySchema));
        Assert.True(service.HasFeature(FeatureKey.ManualCardEntry));
        Assert.False(service.HasFeature(FeatureKey.AiCardRecognition));
    }

    [Fact]
    public void ProTierPlusCardOpsAddon_UnlocksCardOpsAiCapabilities()
    {
        var service = new EntitlementService(new EntitlementOptions
        {
            Tier = EntitlementTier.Pro,
            Addons = [FeatureCatalog.CardOpsAddon]
        });

        Assert.True(service.HasFeature(FeatureKey.AiCardRecognition));
        Assert.True(service.HasFeature(FeatureKey.CardGradingDetection));
        Assert.True(service.HasFeature(FeatureKey.CardCompPricing));
    }

    [Fact]
    public void AiSuiteAddon_UnlocksAiAcrossEveryPlugin_EvenOnFreeTier()
    {
        var service = new EntitlementService(new EntitlementOptions
        {
            Tier = EntitlementTier.Free,
            Addons = [FeatureCatalog.CardOpsAddon, FeatureCatalog.StamplicityAddon, FeatureCatalog.AiSuiteAddon]
        });

        // Master unlock: AI capabilities of both verticals become available.
        Assert.True(service.HasFeature(FeatureKey.AiCardRecognition));
        Assert.True(service.HasFeature(FeatureKey.AiPhilatelyVisualId));
        Assert.True(service.HasFeature(FeatureKey.StampValuationComps));
        Assert.True(service.HasFeature(FeatureKey.AiVisionSuite));
    }

    [Fact]
    public void SignedLicenseKey_ActivatesProTier()
    {
        const string secret = "unit-test-secret";
        var key = LicenseKeySigner.Sign(secret, new LicensePayload("buyer@example.com", EntitlementTier.Pro, [], null));
        var validator = new HmacLicenseKeyValidator(secret);
        var service = new EntitlementService(new EntitlementOptions(), validator);

        Assert.True(service.TryApplyLicense(key, out var message));
        Assert.Equal("license", service.Snapshot.Source);
        Assert.True(service.HasFeature(FeatureKey.BulkApiPublishing));
        Assert.Contains("valid", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TamperedLicenseKey_IsRejected_AndLeavesTierUnchanged()
    {
        const string secret = "unit-test-secret";
        var key = LicenseKeySigner.Sign(secret, new LicensePayload("buyer@example.com", EntitlementTier.Pro, [], null));
        var validator = new HmacLicenseKeyValidator(secret);
        var service = new EntitlementService(new EntitlementOptions(), validator);

        var tampered = key[..^2] + (key.EndsWith("aa") ? "bb" : "aa");
        Assert.False(service.TryApplyLicense(tampered, out _));
        Assert.False(service.HasFeature(FeatureKey.BulkApiPublishing));
    }

    [Fact]
    public void ExpiredLicenseKey_IsRejected()
    {
        const string secret = "unit-test-secret";
        var clock = new TestClock();
        var key = LicenseKeySigner.Sign(secret, new LicensePayload("buyer@example.com", EntitlementTier.Pro, [], clock.Now.AddDays(-1)));
        var validator = new HmacLicenseKeyValidator(secret, clock.Read);

        Assert.False(validator.Validate(key).IsValid);
    }

    [Fact]
    public void Trial_GrantsProUntilItExpires()
    {
        var clock = new TestClock();
        var service = new EntitlementService(new EntitlementOptions
        {
            TrialDuration = TimeSpan.FromDays(14),
            Clock = clock.Read
        });

        Assert.True(service.Snapshot.IsTrial);
        Assert.True(service.HasFeature(FeatureKey.BulkApiPublishing));

        clock.Now = clock.Now.AddDays(15);
        Assert.False(service.HasFeature(FeatureKey.BulkApiPublishing));
        Assert.False(service.Snapshot.IsTrial);
    }

    [Fact]
    public void DevelopmentUnlock_GrantsEverything()
    {
        var service = new EntitlementService(new EntitlementOptions { DevelopmentUnlock = true });

        Assert.Equal("development", service.Snapshot.Source);
        Assert.True(service.HasFeature(FeatureKey.AiVisionSuite));
        Assert.True(service.IsAddonActive(FeatureCatalog.CardOpsAddon));
    }

    [Fact]
    public void SetTier_OverrideRaisesChangedEvent()
    {
        var service = new EntitlementService(new EntitlementOptions());
        var raised = 0;
        service.Changed += (_, _) => raised++;

        service.SetTier(EntitlementTier.Pro);

        Assert.Equal(1, raised);
        Assert.True(service.HasFeature(FeatureKey.BulkApiPublishing));
    }
}
