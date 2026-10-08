using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace eBayHero.Core.Entitlements;

/// <summary>URL-safe base64 helpers (net8.0 has no built-in Base64Url type).</summary>
internal static class Base64Url
{
    public static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => throw new FormatException("Invalid base64url length.")
        };

        return Convert.FromBase64String(padded);
    }
}

/// <summary>
/// Validates offline license keys of the form <c>EH1.&lt;payload&gt;.&lt;signature&gt;</c>.
///
/// The payload is a base64url JSON document ({sub, tier, addons, exp}) and the signature
/// is an HMAC-SHA256 over the encoded payload. Because verification is a pure function of
/// (key, secret) it works fully offline - important for a local-first desktop product -
/// while still preventing trivially forged keys.
/// </summary>
public sealed class HmacLicenseKeyValidator : ILicenseKeyValidator
{
    private const string Prefix = "EH1";

    private readonly byte[] _secretBytes;
    private readonly Func<DateTimeOffset> _clock;

    public HmacLicenseKeyValidator(string secret, Func<DateTimeOffset>? clock = null)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("A license signing secret is required.", nameof(secret));
        }

        _secretBytes = Encoding.UTF8.GetBytes(secret);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public LicenseValidationResult Validate(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return LicenseValidationResult.Invalid("License key is empty.");
        }

        var parts = licenseKey.Trim().Split('.');
        if (parts.Length != 3 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Invalid("License key is not in the expected EH1.payload.signature format.");
        }

        byte[] providedSignature;
        try
        {
            providedSignature = Base64Url.Decode(parts[2]);
        }
        catch (FormatException)
        {
            return LicenseValidationResult.Invalid("License signature is not valid base64url.");
        }

        var expectedSignature = ComputeSignature(parts[1]);
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature))
        {
            return LicenseValidationResult.Invalid("License signature does not match.");
        }

        LicensePayloadDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<LicensePayloadDto>(Base64Url.Decode(parts[1]));
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return LicenseValidationResult.Invalid("License payload could not be decoded.");
        }

        if (dto is null)
        {
            return LicenseValidationResult.Invalid("License payload was empty.");
        }

        var expires = dto.Exp is { } unix
            ? DateTimeOffset.FromUnixTimeSeconds(unix)
            : (DateTimeOffset?)null;

        if (expires is { } expiry && expiry <= _clock())
        {
            return LicenseValidationResult.Invalid("License key has expired.");
        }

        var tier = dto.Tier?.ToLowerInvariant() switch
        {
            "business" => EntitlementTier.Business,
            "pro" or "premium" or "paid" => EntitlementTier.Pro,
            _ => EntitlementTier.Free
        };

        return new LicenseValidationResult(
            true,
            new LicensePayload(dto.Sub ?? string.Empty, tier, dto.Addons ?? [], expires),
            "License key is valid.");
    }

    private byte[] ComputeSignature(string encodedPayload)
    {
        using var hmac = new HMACSHA256(_secretBytes);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedPayload));
    }
}

/// <summary>
/// Permissive validator for local development and the interactive demo. It never fails
/// and derives the tier from the key text so testers can switch plans without a signing
/// secret. Never ship this in a public build.
/// </summary>
public sealed class LocalMockLicenseKeyValidator : ILicenseKeyValidator
{
    public LicenseValidationResult Validate(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return LicenseValidationResult.Invalid("License key is empty.");
        }

        var normalized = licenseKey.Trim().ToLowerInvariant();
        var tier = normalized.Contains("business")
            ? EntitlementTier.Business
            : normalized.Contains("free")
                ? EntitlementTier.Free
                : EntitlementTier.Pro;

        return new LicenseValidationResult(
            true,
            new LicensePayload("local-tester", tier, [], null),
            "Local mock license accepted.");
    }
}

/// <summary>
/// Helper used by tests, the CLI, and documentation to mint signed keys. Production
/// issuance happens on a licensing server; this keeps the format reproducible offline.
/// </summary>
public static class LicenseKeySigner
{
    public static string Sign(string secret, LicensePayload payload)
    {
        var dto = new LicensePayloadDto
        {
            Sub = payload.Subject,
            Tier = payload.Tier.ToString().ToLowerInvariant(),
            Addons = payload.Addons.ToList(),
            Exp = payload.ExpiresUtc?.ToUnixTimeSeconds()
        };

        var encodedPayload = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(dto));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedPayload));
        return $"EH1.{encodedPayload}.{Base64Url.Encode(signature)}";
    }
}

