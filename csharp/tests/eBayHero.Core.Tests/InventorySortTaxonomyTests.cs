using eBayHero.Core.Models;
using eBayHero.Core.Services;

namespace eBayHero.Core.Tests;

public sealed class InventorySortTaxonomyTests
{
    [Fact]
    public void SportsCards_RouteToSportsAndSportSubdirectory()
    {
        var item = new InventoryItem { Category = "Sports Cards", SportOrGame = "Baseball" };

        var routed = InventorySortTaxonomy.TryCreatePlan(item, [], out var plan);

        Assert.True(routed);
        Assert.Equal(Path.Combine("cards", "sports", "baseball"), plan.RelativeDirectory);
    }

    [Fact]
    public void SportsCardsWithoutSport_RouteToSportsRoot()
    {
        var item = new InventoryItem { Category = "Sports Cards" };

        var routed = InventorySortTaxonomy.TryCreatePlan(item, [], out var plan);

        Assert.True(routed);
        Assert.Equal(Path.Combine("cards", "sports"), plan.RelativeDirectory);
    }

    [Fact]
    public void TcgCcg_RoutesPokemonToGameSubdirectory()
    {
        var item = new InventoryItem { Category = "TCG/CCG", SportOrGame = "Pokemon" };

        var routed = InventorySortTaxonomy.TryCreatePlan(item, [], out var plan);

        Assert.True(routed);
        Assert.Equal(Path.Combine("cards", "tcg-ccg", "pokemon"), plan.RelativeDirectory);
    }

    [Fact]
    public void Stamps_UseCountryTagOnlyWhenCountryIsRecognized()
    {
        var item = new InventoryItem { Category = "Stamps", SportOrGame = "Not a country" };

        var routed = InventorySortTaxonomy.TryCreatePlan(item, ["Canada"], out var plan);

        Assert.True(routed);
        Assert.Equal(Path.Combine("stamps", "canada"), plan.RelativeDirectory);
    }

    [Fact]
    public void StampsWithoutCountry_RouteToStampsRoot()
    {
        var item = new InventoryItem { Category = "Stamps" };

        var routed = InventorySortTaxonomy.TryCreatePlan(item, ["airmail"], out var plan);

        Assert.True(routed);
        Assert.Equal("stamps", plan.RelativeDirectory);
    }

    [Fact]
    public void ToBeListedItems_CopyToEbayTempUnlessAlreadyListed()
    {
        var item = new InventoryItem { Category = "Comics", ListingStatus = ListingStatus.NotListed };

        var routed = InventorySortTaxonomy.TryCreatePlan(item, ["to be listed"], out var plan);

        Assert.True(routed);
        Assert.True(plan.CopyToEbayTemp);

        item.ListingStatus = ListingStatus.CurrentlyListed;
        routed = InventorySortTaxonomy.TryCreatePlan(item, ["to be listed"], out plan);

        Assert.True(routed);
        Assert.False(plan.CopyToEbayTemp);
    }
}

