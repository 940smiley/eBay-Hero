namespace eBayHero.Core.Entitlements;

/// <summary>
/// Read-only view of the current entitlements plus the small set of mutations the app
/// needs (activate a license, toggle a tier for local testing).
///
/// Everything that gates a capability depends on this abstraction, never on a concrete
/// tier check, so billing, trials, and addons can evolve without touching workflows.
/// </summary>
public interface IEntitlementService
{
    EntitlementSnapshot Snapshot { get; }

    bool HasFeature(FeatureKey key);

    bool IsAddonActive(string addonId);

    /// <summary>Attempts to activate a license key. Returns false (with a reason) when invalid.</summary>
    bool TryApplyLicense(string licenseKey, out string message);

    /// <summary>Activates a license key or throws when the key is invalid.</summary>
    void ApplyLicense(string licenseKey);

    /// <summary>Overrides the tier/addons directly (local testing, demo presets).</summary>
    void SetTier(EntitlementTier tier, IEnumerable<string>? addons = null);

    /// <summary>Removes any activated license and returns to the configured base state.</summary>
    void ClearLicense();

    event EventHandler? Changed;
}

/// <summary>
/// Default entitlement controller. Resolution order:
/// 1. Development unlock (internal builds) grants everything.
/// 2. A validated license key.
/// 3. An active trial window.
/// 4. The configured base tier (Free by default).
/// </summary>
public sealed class EntitlementService : IEntitlementService
{
    private readonly EntitlementOptions _options;
    private readonly ILicenseKeyValidator _validator;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _gate = new();
    private readonly DateTimeOffset _startedUtc;

    private EntitlementTier _tier;
    private List<string> _addons;
    private string _source;
    private DateTimeOffset? _expiresUtc;
    private bool _licenseActive;

    public EntitlementService(EntitlementOptions options, ILicenseKeyValidator? validator = null)
    {
        _options = options;
        _validator = validator ?? new LocalMockLicenseKeyValidator();
        _clock = options.Clock ?? (() => DateTimeOffset.UtcNow);
        _startedUtc = _clock();
        _tier = options.Tier;
        _addons = Normalize(options.Addons);
        _source = options.Tier == EntitlementTier.Free ? "free" : "configured";

        if (options.DevelopmentUnlock)
        {
            _tier = EntitlementTier.Pro;
            _addons = [FeatureCatalog.CardOpsAddon, FeatureCatalog.StamplicityAddon, FeatureCatalog.AiSuiteAddon];
            _source = "development";
        }
        else if (!string.IsNullOrWhiteSpace(options.LicenseKey))
        {
            var result = _validator.Validate(options.LicenseKey);
            if (result.IsValid && result.Payload is { } payload)
            {
                ApplyPayload(payload);
            }
        }
    }

    public event EventHandler? Changed;

    public EntitlementSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                if (_options.DevelopmentUnlock)
                {
                    return EntitlementEvaluator.Everything(_addons);
                }

                if (_licenseActive)
                {
                    return EntitlementEvaluator.Evaluate(_tier, _addons, "license", _expiresUtc);
                }

                if (_options.TrialDuration is { } trialDuration)
                {
                    var trialEnds = _startedUtc + trialDuration;
                    if (_clock() < trialEnds)
                    {
                        return EntitlementEvaluator.Evaluate(EntitlementTier.Pro, _addons, "trial", null, true, trialEnds);
                    }
                }

                return EntitlementEvaluator.Evaluate(_tier, _addons, _source, _expiresUtc);
            }
        }
    }

    public bool HasFeature(FeatureKey key) => Snapshot.HasFeature(key);

    public bool IsAddonActive(string addonId) => Snapshot.HasAddon(addonId);

    public bool TryApplyLicense(string licenseKey, out string message)
    {
        var result = _validator.Validate(licenseKey);
        if (!result.IsValid || result.Payload is not { } payload)
        {
            message = result.Message;
            return false;
        }

        lock (_gate)
        {
            ApplyPayload(payload);
        }

        message = result.Message;
        return true;
    }

    public void ApplyLicense(string licenseKey)
    {
        if (!TryApplyLicense(licenseKey, out var message))
        {
            throw new InvalidOperationException(message);
        }
    }

    public void SetTier(EntitlementTier tier, IEnumerable<string>? addons = null)
    {
        lock (_gate)
        {
            _tier = tier;
            _addons = addons is null ? _addons : Normalize(addons);
            _source = "override";
            _licenseActive = false;
            _expiresUtc = null;
        }

        RaiseChanged();
    }

    public void ClearLicense()
    {
        lock (_gate)
        {
            _tier = _options.Tier;
            _addons = Normalize(_options.Addons);
            _source = _options.Tier == EntitlementTier.Free ? "free" : "configured";
            _licenseActive = false;
            _expiresUtc = null;
        }

        RaiseChanged();
    }

    private void ApplyPayload(LicensePayload payload)
    {
        _tier = payload.Tier;
        // Addons from the license are merged with locally enabled addons so a user who
        // enabled a plugin manually keeps it after activating a core license.
        _addons = Normalize(_addons.Concat(payload.Addons));
        _expiresUtc = payload.ExpiresUtc;
        _source = "license";
        _licenseActive = true;
        RaiseChanged();
    }

    private static List<string> Normalize(IEnumerable<string> addons) =>
        addons
            .Where(addon => !string.IsNullOrWhiteSpace(addon))
            .Select(addon => addon.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}

