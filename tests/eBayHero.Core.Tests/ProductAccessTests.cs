using eBayHero.Core.Licensing;

namespace eBayHero.Core.Tests;

public sealed class ProductAccessTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "EbayHeroTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void PublicTrial_AllowsExactlyConfiguredPremiumActions()
    {
        var service = new ProductAccessService(_root, ProductEdition.Public);
        for (var index = 0; index < ProductAccessService.TrialPremiumActions; index++) Assert.True(service.TryConsumePremiumAction(out _));
        Assert.False(service.TryConsumePremiumAction(out var message));
        Assert.Contains("trial has ended", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DevelopmentEdition_BypassesTrialCounter()
    {
        var service = new ProductAccessService(_root, ProductEdition.Development);
        for (var index = 0; index < 100; index++) Assert.True(service.TryConsumePremiumAction(out _));
        Assert.Equal(0, service.Current.PremiumActionsUsed);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
