using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;

namespace eBayHero.Plugins.Tests;

public sealed class PluginManifestTests
{
    private const string ValidManifest = """
    {
      "schemaVersion": 1,
      "id": "cardops",
      "name": "CardOps",
      "version": "1.0.0",
      "tier": "free",
      "entryPoint": "eBayHero.Plugins.CardOps.dll",
      "capabilities": [
        { "id": "card.inventory.schema", "name": "Card inventory schema", "requiredFeature": "CardInventorySchema" }
      ],
      "hooks": [ { "name": "inventory.item.created", "order": 10 } ],
      "routes": [ { "method": "POST", "path": "/plugins/cardops/draft", "handler": "ExportDraft", "requiredFeature": "CardDraftExport" } ]
    }
    """;

    [Fact]
    public void ValidManifest_Parses()
    {
        var result = PluginManifestLoader.Parse(ValidManifest);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Manifest);
        Assert.Equal("cardops", result.Manifest!.Id);
        Assert.Equal(PluginTier.Free, result.Manifest.Tier);
        Assert.Equal(FeatureKey.CardInventorySchema, result.Manifest.Capabilities[0].RequiredFeature);
        Assert.Single(result.Manifest.Routes);
    }

    [Fact]
    public void Manifest_MissingId_IsInvalid()
    {
        var result = PluginManifestLoader.Parse("""{ "schemaVersion": 1, "name": "X", "version": "1.0.0" }""");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manifest_UnsupportedSchemaVersion_IsInvalid()
    {
        var result = PluginManifestLoader.Parse("""{ "schemaVersion": 99, "id": "x", "name": "X", "version": "1.0.0" }""");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("schemaVersion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manifest_RelativeRoutePath_IsInvalid()
    {
        var result = PluginManifestLoader.Parse("""
        {
          "schemaVersion": 1, "id": "x", "name": "X", "version": "1.0.0",
          "routes": [ { "method": "GET", "path": "plugins/x/status" } ]
        }
        """);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("absolute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manifest_UnknownRequiredFeature_IsInvalid()
    {
        var result = PluginManifestLoader.Parse("""
        {
          "schemaVersion": 1, "id": "x", "name": "X", "version": "1.0.0",
          "capabilities": [ { "id": "a", "requiredFeature": "NotAFeature" } ]
        }
        """);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("unknown feature", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manifest_MalformedJson_IsInvalid()
    {
        var result = PluginManifestLoader.Parse("{ not json ");

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
