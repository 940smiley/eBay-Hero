using System.Text.Json;

namespace eBayHero.Core.Plugins;

/// <summary>
/// Small JSON helpers shared by plugins so each addon doesn't re-implement body parsing.
/// Deliberately lenient: malformed bodies yield an empty dictionary rather than an
/// exception, because a bad request should never crash the plugin host.
/// </summary>
public static class PluginJson
{
    public static IReadOnlyDictionary<string, object?> ParseBody(string? body)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(body))
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                result[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt64(out var integer) ? integer : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => property.Value.ToString()
                };
            }
        }
        catch (JsonException)
        {
            // Ignore malformed bodies; callers validate required fields themselves.
        }

        return result;
    }

    public static string ToJson(object? value) => JsonSerializer.Serialize(value);
}
