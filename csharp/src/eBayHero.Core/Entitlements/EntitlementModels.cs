using System.Text.Json;
using System.Text.Json.Serialization;

namespace eBayHero.Core.Entitlements;

/// <summary>Subscription tier for the core eBay Hero product.</summary>
public enum EntitlementTier
{
    Free = 0,
    Pro = 1,
    Business = 2
}

/// <summary>
/// The decoded contents of a signed license key. Kept deliberately small and portable so
/// the same key can be validated by the desktop app, the web companion, or an addon.
/// </summary>
public sealed record LicensePayload(
    string Subject,
    EntitlementTier Tier,
    IReadOnlyList<string> Addons,
    DateTimeOffset? ExpiresUtc);

/// <summary>
/// Immutable view of what the current install is entitled to. This is the only object
/// business code should consult for gating decisions.
/// </summary>
public sealed record EntitlementSnapshot(
    EntitlementTier Tier,
    IReadOnlyList<string> Addons,
    IReadOnlySet<FeatureKey> Features,
    string Source,
    DateTimeOffset? ExpiresUtc = null,
    bool IsTrial = false,
    DateTimeOffset? TrialEndsUtc = null)
{
    public bool IsPro => Tier >= EntitlementTier.Pro;

    public bool HasFeature(FeatureKey key) => Features.Contains(key);

    public bool HasAddon(string addonId) =>
        Addons.Contains(addonId, StringComparer.OrdinalIgnoreCase);

    /// <summary>A snapshot with nothing but the free core features.</summary>
    public static EntitlementSnapshot Free { get; } = EntitlementEvaluator.Evaluate(
        EntitlementTier.Free,
        [],
        "free");
}

/// <summary>Configuration used to construct an <see cref="EntitlementService"/>.</summary>
public sealed record EntitlementOptions
{
    /// <summary>Base tier when no license key is supplied.</summary>
    public EntitlementTier Tier { get; init; } = EntitlementTier.Free;

    /// <summary>Addon ids that are enabled locally (e.g. <c>cardops</c>, <c>ai-suite</c>).</summary>
    public IReadOnlyList<string> Addons { get; init; } = [];

    /// <summary>Optional signed license key; overrides <see cref="Tier"/> when valid.</summary>
    public string? LicenseKey { get; init; }

    /// <summary>
    /// Developer/QA override that grants Pro without a license. Mirrors the existing
    /// <c>Development</c> build flavor so internal testing never hits a paywall.
    /// </summary>
    public bool DevelopmentUnlock { get; init; }

    /// <summary>When set, a one-time trial window is granted from first use.</summary>
    public TimeSpan? TrialDuration { get; init; }

    /// <summary>Injectable clock for deterministic tests.</summary>
    public Func<DateTimeOffset>? Clock { get; init; }
}

/// <summary>Result of validating a license key.</summary>
public sealed record LicenseValidationResult(bool IsValid, LicensePayload? Payload, string Message)
{
    public static LicenseValidationResult Invalid(string message) => new(false, null, message);
}

/// <summary>Validates signed license keys. Swap the implementation for offline/online checks.</summary>
public interface ILicenseKeyValidator
{
    LicenseValidationResult Validate(string licenseKey);
}

/// <summary>
/// JSON shape of the license payload as serialized by the signing tool. Kept separate
/// from <see cref="LicensePayload"/> so wire format and domain model can evolve apart.
/// </summary>
internal sealed class LicensePayloadDto
{
    [JsonPropertyName("sub")] public string Sub { get; set; } = string.Empty;
    [JsonPropertyName("tier")] public string Tier { get; set; } = "pro";
    [JsonPropertyName("addons")] public List<string> Addons { get; set; } = [];
    [JsonPropertyName("exp")] public long? Exp { get; set; }
}
