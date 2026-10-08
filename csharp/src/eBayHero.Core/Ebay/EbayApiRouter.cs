using eBayHero.Core.Entitlements;

namespace eBayHero.Core.Ebay;

/// <summary>Which eBay API family a call targets.</summary>
public enum EbayApiSurface
{
    Rest,
    Trading
}

/// <summary>A provider-agnostic eBay API call.</summary>
public sealed record EbayApiCall(
    string Operation,
    string Method,
    string Path,
    string? Body = null,
    EbayApiSurface PreferredSurface = EbayApiSurface.Rest,
    FeatureKey? RequiredFeature = null,
    bool AllowTradingFallback = true);

/// <summary>Normalized eBay API response.</summary>
public sealed record EbayApiResponse(
    bool Success,
    int StatusCode,
    string Body,
    EbayApiSurface Surface,
    string? Error = null,
    int Attempts = 1);

/// <summary>A transport for one eBay API surface (REST or Trading).</summary>
public interface IEbayTransport
{
    EbayApiSurface Surface { get; }

    Task<EbayApiResponse> SendAsync(EbayApiCall call, CancellationToken cancellationToken = default);
}

/// <summary>Retry configuration for transient eBay failures.</summary>
public sealed record EbayRetryPolicy(int MaxAttempts = 3, TimeSpan? BaseDelay = null)
{
    public static EbayRetryPolicy Default { get; } = new();

    public TimeSpan DelayFor(int attempt) => (BaseDelay ?? TimeSpan.FromMilliseconds(250)) * Math.Pow(2, attempt - 1);

    public bool IsTransient(int statusCode) => statusCode == 429 || statusCode >= 500;
}

/// <summary>
/// Routes eBay calls to the REST surface first and falls back to the legacy Trading API
/// when REST does not implement an operation. Adds retry with exponential backoff and
/// applies the entitlement gate so premium operations are refused before any network call.
/// </summary>
public sealed class EbayApiRouter
{
    private readonly IEntitlementService _entitlements;
    private readonly Dictionary<EbayApiSurface, IEbayTransport> _transports;
    private readonly EbayRetryPolicy _retryPolicy;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public EbayApiRouter(
        IEntitlementService entitlements,
        IEnumerable<IEbayTransport> transports,
        EbayRetryPolicy? retryPolicy = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _entitlements = entitlements;
        _transports = transports.ToDictionary(transport => transport.Surface);
        _retryPolicy = retryPolicy ?? EbayRetryPolicy.Default;
        _delay = delay ?? ((span, token) => Task.Delay(span, token));
    }

    public async Task<EbayApiResponse> SendAsync(EbayApiCall call, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);

        if (call.RequiredFeature is { } feature && !_entitlements.HasFeature(feature))
        {
            return new EbayApiResponse(false, 402, string.Empty, call.PreferredSurface, $"Feature '{feature}' is not included in the current plan.", 0);
        }

        if (!_transports.TryGetValue(call.PreferredSurface, out var primary))
        {
            return new EbayApiResponse(false, 501, string.Empty, call.PreferredSurface, $"No transport registered for {call.PreferredSurface}.", 0);
        }

        var primaryResponse = await AttemptAsync(primary, call, cancellationToken);

        if (primaryResponse.Success || !call.AllowTradingFallback)
        {
            return primaryResponse;
        }

        var fallbackSurface = call.PreferredSurface == EbayApiSurface.Rest ? EbayApiSurface.Trading : EbayApiSurface.Rest;
        if (!_transports.TryGetValue(fallbackSurface, out var fallback) || !IsFallbackWorthy(primaryResponse.StatusCode))
        {
            return primaryResponse;
        }

        var fallbackResponse = await AttemptAsync(fallback, call, cancellationToken);
        return fallbackResponse.Success ? fallbackResponse : primaryResponse;
    }

    /// <summary>Trading API is used as a fallback for operations REST does not expose.</summary>
    private static bool IsFallbackWorthy(int statusCode) => statusCode is 404 or 405 or 501;

    private async Task<EbayApiResponse> AttemptAsync(IEbayTransport transport, EbayApiCall call, CancellationToken cancellationToken)
    {
        EbayApiResponse response = new(false, 0, string.Empty, transport.Surface);
        for (var attempt = 1; attempt <= _retryPolicy.MaxAttempts; attempt++)
        {
            response = await transport.SendAsync(call, cancellationToken);
            response = response with { Attempts = attempt };

            if (response.Success || !_retryPolicy.IsTransient(response.StatusCode) || attempt == _retryPolicy.MaxAttempts)
            {
                return response;
            }

            await _delay(_retryPolicy.DelayFor(attempt), cancellationToken);
        }

        return response;
    }
}
