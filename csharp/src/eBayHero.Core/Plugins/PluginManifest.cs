using System.Text.Json;
using System.Text.Json.Serialization;
using eBayHero.Core.Entitlements;

namespace eBayHero.Core.Plugins;

/// <summary>
/// Declarative description of a plugin as shipped on disk (PluginManifest.json, schema
/// v1). Third-party addons can be discovered and validated without loading their code,
/// which keeps the host resilient to malformed or hostile manifests.
/// </summary>
public sealed record PluginManifest(
    int SchemaVersion,
    string Id,
    string Name,
    string Version,
    PluginTier Tier,
    string? EntryPoint,
    IReadOnlyList<string> Addons,
    IReadOnlyList<PluginCapability> Capabilities,
    IReadOnlyList<PluginHook> Hooks,
    IReadOnlyList<PluginRoute> Routes);

/// <summary>Outcome of parsing a manifest: either a manifest or the list of problems.</summary>
public sealed record ManifestValidationResult(bool IsValid, PluginManifest? Manifest, IReadOnlyList<string> Errors)
{
    public static ManifestValidationResult Invalid(params string[] errors) => new(false, null, errors);
}

/// <summary>Parses and validates PluginManifest.json documents.</summary>
public static class PluginManifestLoader
{
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ManifestValidationResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ManifestValidationResult.Invalid("Manifest is empty.");
        }

        PluginManifestDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PluginManifestDto>(json, Options);
        }
        catch (JsonException exception)
        {
            return ManifestValidationResult.Invalid($"Manifest is not valid JSON: {exception.Message}");
        }

        if (dto is null)
        {
            return ManifestValidationResult.Invalid("Manifest was null.");
        }

        var errors = new List<string>();
        if (dto.SchemaVersion != SupportedSchemaVersion)
        {
            errors.Add($"Unsupported schemaVersion '{dto.SchemaVersion}'; expected {SupportedSchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            errors.Add("Plugin id is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("Plugin name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Version))
        {
            errors.Add("Plugin version is required.");
        }

        var capabilities = new List<PluginCapability>();
        foreach (var capability in dto.Capabilities ?? [])
        {
            if (string.IsNullOrWhiteSpace(capability.Id))
            {
                errors.Add("Every capability requires an id.");
                continue;
            }

            FeatureKey? required = null;
            if (!string.IsNullOrWhiteSpace(capability.RequiredFeature))
            {
                if (Enum.TryParse<FeatureKey>(capability.RequiredFeature, ignoreCase: true, out var parsed))
                {
                    required = parsed;
                }
                else
                {
                    errors.Add($"Capability '{capability.Id}' references unknown feature '{capability.RequiredFeature}'.");
                }
            }

            capabilities.Add(new PluginCapability(capability.Id, capability.Name ?? capability.Id, required, capability.Description ?? string.Empty));
        }

        if (capabilities.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != capabilities.Count)
        {
            errors.Add("Capability ids must be unique within a plugin.");
        }

        var hooks = (dto.Hooks ?? [])
            .Where(hook => !string.IsNullOrWhiteSpace(hook.Name))
            .Select(hook => new PluginHook(hook.Name!, hook.Description ?? string.Empty, hook.Order))
            .ToList();

        var routes = new List<PluginRoute>();
        foreach (var route in dto.Routes ?? [])
        {
            if (string.IsNullOrWhiteSpace(route.Path) || !route.Path.StartsWith('/'))
            {
                errors.Add($"Route path '{route.Path}' must be absolute (start with '/').");
                continue;
            }

            FeatureKey? required = null;
            if (!string.IsNullOrWhiteSpace(route.RequiredFeature) &&
                Enum.TryParse<FeatureKey>(route.RequiredFeature, ignoreCase: true, out var parsed))
            {
                required = parsed;
            }

            routes.Add(new PluginRoute(route.Method ?? "GET", route.Path!, route.Handler ?? string.Empty, required));
        }

        if (errors.Count > 0)
        {
            return new ManifestValidationResult(false, null, errors);
        }

        var manifest = new PluginManifest(
            dto.SchemaVersion,
            dto.Id!,
            dto.Name!,
            dto.Version!,
            ParseTier(dto.Tier),
            dto.EntryPoint,
            dto.Addons ?? [],
            capabilities,
            hooks,
            routes);

        return new ManifestValidationResult(true, manifest, []);
    }

    private static PluginTier ParseTier(string? tier) => tier?.ToLowerInvariant() switch
    {
        "core" => PluginTier.Core,
        "pro" or "paid" or "premium" => PluginTier.Pro,
        _ => PluginTier.Free
    };

    private sealed class PluginManifestDto
    {
        [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } = SupportedSchemaVersion;
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("version")] public string? Version { get; set; }
        [JsonPropertyName("tier")] public string? Tier { get; set; }
        [JsonPropertyName("entryPoint")] public string? EntryPoint { get; set; }
        [JsonPropertyName("addons")] public List<string>? Addons { get; set; }
        [JsonPropertyName("capabilities")] public List<CapabilityDto>? Capabilities { get; set; }
        [JsonPropertyName("hooks")] public List<HookDto>? Hooks { get; set; }
        [JsonPropertyName("routes")] public List<RouteDto>? Routes { get; set; }
    }

    private sealed class CapabilityDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("requiredFeature")] public string? RequiredFeature { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    private sealed class HookDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("order")] public int Order { get; set; }
    }

    private sealed class RouteDto
    {
        [JsonPropertyName("method")] public string? Method { get; set; }
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("handler")] public string? Handler { get; set; }
        [JsonPropertyName("requiredFeature")] public string? RequiredFeature { get; set; }
    }
}
