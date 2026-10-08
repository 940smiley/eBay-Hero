using System.Text;
using System.Text.Json;

namespace eBayHero.Core.Ebay;

/// <summary>An access/refresh token pair returned by the eBay OAuth endpoints.</summary>
public sealed record EbayTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresUtc,
    IReadOnlyList<string> Scopes)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresUtc;
}

/// <summary>Exchanges authorization codes and refresh tokens for access tokens.</summary>
public interface IEbayTokenClient
{
    Task<EbayTokenSet> ExchangeCodeAsync(EbaySettings settings, string authorizationCode, CancellationToken cancellationToken = default);

    Task<EbayTokenSet> RefreshAsync(EbaySettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pure helpers for the eBay OAuth 2.0 authorization-code flow. Keeping URL/body/response
/// construction as static functions means the tricky parts (scope joining, Basic auth,
/// expiry math) are unit-testable without any network access.
/// </summary>
public static class EbayOAuthFlow
{
    public const string TokenPath = "/identity/v1/oauth2/token";

    public static string BuildAuthorizationUrl(EbaySettings settings, string state)
    {
        if (string.IsNullOrWhiteSpace(settings.ClientId))
        {
            throw new ArgumentException("Client ID is required to build an authorization URL.", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(settings.RuName))
        {
            throw new ArgumentException("RuName is required to build an authorization URL.", nameof(settings));
        }

        var scopes = settings.Scopes.Count == 0 ? EbayScopes.MinimalSeller : settings.Scopes;
        var query = new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = settings.RuName,
            ["response_type"] = "code",
            ["scope"] = string.Join(' ', scopes),
            ["state"] = state
        };

        return settings.AuthBaseUrl + "/oauth2/authorize?" +
            string.Join('&', query.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
    }

    public static string BuildTokenEndpoint(EbaySettings settings) => settings.ApiBaseUrl + TokenPath;

    /// <summary>HTTP Basic credentials are base64(clientId:clientSecret).</summary>
    public static string BuildBasicAuthorizationHeader(EbaySettings settings)
    {
        var raw = $"{settings.ClientId}:{settings.ClientSecret}";
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static string BuildAuthorizationCodeBody(string authorizationCode) =>
        "grant_type=authorization_code&code=" + Uri.EscapeDataString(authorizationCode);

    public static string BuildRefreshBody(string refreshToken) =>
        "grant_type=refresh_token&refresh_token=" + Uri.EscapeDataString(refreshToken);

    public static EbayTokenSet ParseTokenResponse(
        string json,
        DateTimeOffset now,
        IReadOnlyList<string>? fallbackScopes = null,
        string? previousRefreshToken = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var accessToken = ReadString(root, "access_token")
            ?? throw new InvalidOperationException("eBay token response did not include an access_token.");

        // Refresh responses omit refresh_token; reuse the previous one in that case.
        var refreshToken = ReadString(root, "refresh_token") ?? previousRefreshToken ?? string.Empty;

        var expiresIn = root.TryGetProperty("expires_in", out var expiresElement) && expiresElement.TryGetInt64(out var seconds)
            ? seconds
            : 7200;

        var scopes = ReadString(root, "scope")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList()
            ?? fallbackScopes?.ToList()
            ?? [];

        return new EbayTokenSet(accessToken, refreshToken, now.AddSeconds(expiresIn), scopes);
    }

    private static string? ReadString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}

/// <summary>Default HTTP implementation of <see cref="IEbayTokenClient"/>.</summary>
public sealed class HttpEbayTokenClient(HttpClient httpClient, Func<DateTimeOffset>? clock = null) : IEbayTokenClient
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    public async Task<EbayTokenSet> ExchangeCodeAsync(EbaySettings settings, string authorizationCode, CancellationToken cancellationToken = default)
    {
        var body = EbayOAuthFlow.BuildAuthorizationCodeBody(authorizationCode);
        var json = await PostAsync(settings, body, cancellationToken);
        return EbayOAuthFlow.ParseTokenResponse(json, _clock(), settings.Scopes);
    }

    public async Task<EbayTokenSet> RefreshAsync(EbaySettings settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.RefreshToken))
        {
            throw new InvalidOperationException("A refresh token is required to refresh access.");
        }

        var body = EbayOAuthFlow.BuildRefreshBody(settings.RefreshToken);
        var json = await PostAsync(settings, body, cancellationToken);
        return EbayOAuthFlow.ParseTokenResponse(json, _clock(), settings.Scopes, settings.RefreshToken);
    }

    private async Task<string> PostAsync(EbaySettings settings, string body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EbayOAuthFlow.BuildTokenEndpoint(settings))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.TryAddWithoutValidation("Authorization", EbayOAuthFlow.BuildBasicAuthorizationHeader(settings));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"eBay token request failed with {(int)response.StatusCode}: {json}");
        }

        return json;
    }
}

