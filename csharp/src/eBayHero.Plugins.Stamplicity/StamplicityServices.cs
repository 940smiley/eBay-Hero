namespace eBayHero.Plugins.Stamplicity;

/// <summary>
/// AI philately visual identification entry point. Ships as a deterministic local stub;
/// a production build replaces the body with a vision-service call. The payload shape is
/// the contract the demo and the UI depend on.
/// </summary>
public static class PhilatelyRecognition
{
    public static object Simulate(string? imagePath)
    {
        var fileName = string.IsNullOrWhiteSpace(imagePath) ? "(none)" : Path.GetFileName(imagePath);
        return new
        {
            plugin = StamplicityPlugin.PluginId,
            engine = "local-stub",
            image = fileName,
            confidence = 0.88,
            perforations = new { count = 11, measurement = "11 x 11", confidence = 0.86 },
            watermark = new { name = "Crown CA", detected = true, confidence = 0.79 },
            centering = new { horizontalPercent = 45, verticalPercent = 52, grade = "F-VF" },
            catalog = new { scott = "594", stanleyGibbons = "SG 589" },
            needsReview = false
        };
    }
}

/// <summary>
/// Automated stamp valuation. Uses authorized sold comparables only, mirroring the core
/// pricing policy that active asking prices are not sold values.
/// </summary>
public static class StampValuation
{
    public static object Simulate(string? catalogNumber)
    {
        return new
        {
            plugin = StamplicityPlugin.PluginId,
            engine = "local-stub",
            catalogNumber = string.IsNullOrWhiteSpace(catalogNumber) ? "Unknown" : catalogNumber,
            comparableCount = 4,
            low = 4.25m,
            median = 9.50m,
            high = 18.00m,
            confidence = "low",
            evidenceSource = "authorized-sold-comparables"
        };
    }
}
