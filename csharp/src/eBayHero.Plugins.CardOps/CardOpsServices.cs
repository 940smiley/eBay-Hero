namespace eBayHero.Plugins.CardOps;

/// <summary>
/// Card recognition/OCR entry point.
///
/// This ships a deterministic local stub so the workflow, routes, and entitlement gates
/// are exercisable without an AI provider. A production deployment swaps the body of
/// <see cref="Simulate"/> for a call to the OCR/AI service; the contract (route + payload
/// shape) stays identical, which is the point of the plugin boundary.
/// </summary>
public static class CardRecognition
{
    public static object Simulate(string? imagePath)
    {
        var fileName = string.IsNullOrWhiteSpace(imagePath) ? "(none)" : Path.GetFileName(imagePath);
        return new
        {
            plugin = CardOpsPlugin.PluginId,
            engine = "local-stub",
            image = fileName,
            confidence = 0.92,
            fields = new Dictionary<string, string>
            {
                ["year"] = "2023",
                ["brand"] = "Topps Chrome",
                ["player"] = "Ruben Amaro",
                ["cardNumber"] = "12",
                ["serial"] = "23/99",
                ["rookie"] = "true",
                ["autograph"] = "true"
            },
            grading = new { company = "PSA", grade = "9", detected = true },
            needsReview = false
        };
    }
}

/// <summary>
/// Comp pricing entry point. Only authorized sold-comparable evidence is used; active
/// asking prices are never treated as sold values (matching the core pricing policy).
/// </summary>
public static class CardPricing
{
    public static object Simulate(string? player, string? year)
    {
        return new
        {
            plugin = CardOpsPlugin.PluginId,
            engine = "local-stub",
            player = string.IsNullOrWhiteSpace(player) ? "Unknown" : player,
            year = string.IsNullOrWhiteSpace(year) ? "Unknown" : year,
            comparableCount = 6,
            low = 8.50m,
            median = 15.00m,
            high = 27.00m,
            confidence = "medium",
            evidenceSource = "authorized-sold-comparables"
        };
    }
}
