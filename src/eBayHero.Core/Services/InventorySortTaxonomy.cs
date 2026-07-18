using eBayHero.Core.Models;

namespace eBayHero.Core.Services;

public sealed record InventorySortPlan(
    string RelativeDirectory,
    bool CopyToEbayTemp,
    string Reason);

public static class InventorySortTaxonomy
{
    public static IReadOnlyList<string> CategoryOptions { get; } =
    [
        "Books",
        "Comics",
        "Fast Food Toys",
        "Memorabilia",
        "Newspapers",
        "Other",
        "Photos",
        "Postcards",
        "Sports Cards",
        "Stamps",
        "TCG/CCG",
        "Toys",
        "Trading Cards"
    ];

    public static IReadOnlyList<string> SportsCardSports { get; } =
    [
        "Baseball",
        "Basketball",
        "Boxing",
        "Football",
        "Golf",
        "Hockey",
        "MMA",
        "Racing",
        "Soccer",
        "Tennis",
        "Wrestling",
        "Other Sports"
    ];

    public static IReadOnlyList<string> TcgCcgGames { get; } =
    [
        "Pokemon",
        "Dragon Ball Z",
        "Dragon Ball Super",
        "Yu-Gi-Oh!",
        "Magic: The Gathering",
        "One Piece",
        "Disney Lorcana",
        "Star Wars CCG",
        "Flesh and Blood",
        "Digimon",
        "Weiss Schwarz",
        "Cardfight!! Vanguard",
        "MetaZoo",
        "Final Fantasy TCG",
        "Marvel",
        "Duel Masters",
        "Force of Will",
        "Future Card Buddyfight",
        "My Hero Academia",
        "UniVersus",
        "Garbage Pail Kids",
        "Other TCG/CCG Games"
    ];

    public static IReadOnlyList<string> StampCountries { get; } =
    [
        "Afghanistan",
        "Albania",
        "Algeria",
        "Andorra",
        "Angola",
        "Antigua and Barbuda",
        "Argentina",
        "Armenia",
        "Australia",
        "Austria",
        "Azerbaijan",
        "Bahamas",
        "Bahrain",
        "Bangladesh",
        "Barbados",
        "Belarus",
        "Belgium",
        "Belize",
        "Benin",
        "Bhutan",
        "Bolivia",
        "Bosnia and Herzegovina",
        "Botswana",
        "Brazil",
        "Brunei",
        "Bulgaria",
        "Burkina Faso",
        "Burundi",
        "Cabo Verde",
        "Cambodia",
        "Cameroon",
        "Canada",
        "Central African Republic",
        "Chad",
        "Chile",
        "China",
        "Colombia",
        "Comoros",
        "Congo",
        "Costa Rica",
        "Cote d'Ivoire",
        "Croatia",
        "Cuba",
        "Cyprus",
        "Czechia",
        "Democratic Republic of the Congo",
        "Denmark",
        "Djibouti",
        "Dominica",
        "Dominican Republic",
        "Ecuador",
        "Egypt",
        "El Salvador",
        "Equatorial Guinea",
        "Eritrea",
        "Estonia",
        "Eswatini",
        "Ethiopia",
        "Fiji",
        "Finland",
        "France",
        "Gabon",
        "Gambia",
        "Georgia",
        "Germany",
        "Ghana",
        "Greece",
        "Grenada",
        "Guatemala",
        "Guinea",
        "Guinea-Bissau",
        "Guyana",
        "Haiti",
        "Honduras",
        "Hungary",
        "Iceland",
        "India",
        "Indonesia",
        "Iran",
        "Iraq",
        "Ireland",
        "Israel",
        "Italy",
        "Jamaica",
        "Japan",
        "Jordan",
        "Kazakhstan",
        "Kenya",
        "Kiribati",
        "Kuwait",
        "Kyrgyzstan",
        "Laos",
        "Latvia",
        "Lebanon",
        "Lesotho",
        "Liberia",
        "Libya",
        "Liechtenstein",
        "Lithuania",
        "Luxembourg",
        "Madagascar",
        "Malawi",
        "Malaysia",
        "Maldives",
        "Mali",
        "Malta",
        "Marshall Islands",
        "Mauritania",
        "Mauritius",
        "Mexico",
        "Micronesia",
        "Moldova",
        "Monaco",
        "Mongolia",
        "Montenegro",
        "Morocco",
        "Mozambique",
        "Myanmar",
        "Namibia",
        "Nauru",
        "Nepal",
        "Netherlands",
        "New Zealand",
        "Nicaragua",
        "Niger",
        "Nigeria",
        "North Korea",
        "North Macedonia",
        "Norway",
        "Oman",
        "Pakistan",
        "Palau",
        "Panama",
        "Papua New Guinea",
        "Paraguay",
        "Peru",
        "Philippines",
        "Poland",
        "Portugal",
        "Qatar",
        "Romania",
        "Russia",
        "Rwanda",
        "Saint Kitts and Nevis",
        "Saint Lucia",
        "Saint Vincent and the Grenadines",
        "Samoa",
        "San Marino",
        "Sao Tome and Principe",
        "Saudi Arabia",
        "Senegal",
        "Serbia",
        "Seychelles",
        "Sierra Leone",
        "Singapore",
        "Slovakia",
        "Slovenia",
        "Solomon Islands",
        "Somalia",
        "South Africa",
        "South Korea",
        "South Sudan",
        "Spain",
        "Sri Lanka",
        "Sudan",
        "Suriname",
        "Sweden",
        "Switzerland",
        "Syria",
        "Taiwan",
        "Tajikistan",
        "Tanzania",
        "Thailand",
        "Timor-Leste",
        "Togo",
        "Tonga",
        "Trinidad and Tobago",
        "Tunisia",
        "Turkey",
        "Turkmenistan",
        "Tuvalu",
        "Uganda",
        "Ukraine",
        "United Arab Emirates",
        "United Kingdom",
        "United States",
        "Uruguay",
        "Uzbekistan",
        "Vanuatu",
        "Vatican City",
        "Venezuela",
        "Vietnam",
        "Yemen",
        "Zambia",
        "Zimbabwe"
    ];

    private static readonly IReadOnlyDictionary<string, string> TcgAliases = BuildAliasMap(TcgCcgGames, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["mtg"] = "Magic: The Gathering",
        ["magic"] = "Magic: The Gathering",
        ["yugioh"] = "Yu-Gi-Oh!",
        ["yu gi oh"] = "Yu-Gi-Oh!",
        ["dbz"] = "Dragon Ball Z",
        ["dragon ball"] = "Dragon Ball Z",
        ["dragon ball super card game"] = "Dragon Ball Super",
        ["lorcana"] = "Disney Lorcana",
        ["fab"] = "Flesh and Blood",
        ["vanguard"] = "Cardfight!! Vanguard",
        ["other"] = "Other TCG/CCG Games",
        ["other tcg"] = "Other TCG/CCG Games",
        ["other ccg"] = "Other TCG/CCG Games"
    });

    private static readonly IReadOnlyDictionary<string, string> CountryAliases = BuildAliasMap(StampCountries, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["usa"] = "United States",
        ["us"] = "United States",
        ["u s"] = "United States",
        ["u s a"] = "United States",
        ["america"] = "United States",
        ["united states of america"] = "United States",
        ["uk"] = "United Kingdom",
        ["u k"] = "United Kingdom",
        ["great britain"] = "United Kingdom",
        ["england"] = "United Kingdom",
        ["czech republic"] = "Czechia",
        ["drc"] = "Democratic Republic of the Congo",
        ["democratic republic congo"] = "Democratic Republic of the Congo",
        ["republic of congo"] = "Congo",
        ["south korea"] = "South Korea",
        ["north korea"] = "North Korea",
        ["russian federation"] = "Russia",
        ["viet nam"] = "Vietnam"
    });

    public static IReadOnlyList<string> GetSubcategoryOptions() =>
        SportsCardSports
            .Concat(TcgCcgGames)
            .Concat(StampCountries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> GetKnownRelativeDirectories()
    {
        var directories = new List<string>
        {
            Path.Combine("books"),
            Path.Combine("cards"),
            Path.Combine("cards", "sports"),
            Path.Combine("cards", "tcg-ccg"),
            Path.Combine("cards", "trading-cards"),
            Path.Combine("comics"),
            Path.Combine("memorabilia"),
            Path.Combine("newspapers"),
            Path.Combine("other"),
            Path.Combine("photos"),
            Path.Combine("postcards"),
            Path.Combine("stamps"),
            Path.Combine("toys"),
            Path.Combine("toys", "fast-food-toys")
        };

        directories.AddRange(SportsCardSports.Select(sport => Path.Combine("cards", "sports", ToDirectorySegment(sport))));
        directories.AddRange(TcgCcgGames.Select(game => Path.Combine("cards", "tcg-ccg", ToDirectorySegment(game))));
        directories.AddRange(StampCountries.Select(country => Path.Combine("stamps", ToDirectorySegment(country))));

        return directories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool TryCreatePlan(InventoryItem item, IEnumerable<string> tagNames, out InventorySortPlan plan)
    {
        var tags = tagNames
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var category = NormalizeKey(item.Category);
        var subcategory = item.SportOrGame.Trim();
        var relativeDirectory = ResolveRelativeDirectory(category, subcategory, tags);
        if (string.IsNullOrWhiteSpace(relativeDirectory))
        {
            plan = new InventorySortPlan(string.Empty, false, "No category route matched.");
            return false;
        }

        plan = new InventorySortPlan(
            relativeDirectory,
            ShouldCopyToEbayTemp(item, tags),
            $"Category '{item.Category}' routed to '{relativeDirectory}'.");
        return true;
    }

    public static string ToDirectorySegment(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var collapsed = new string(chars);
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(collapsed.Trim('-')) ? "other" : collapsed.Trim('-');
    }

    private static string ResolveRelativeDirectory(string category, string subcategory, IReadOnlyList<string> tags)
    {
        if (IsSportsCategory(category) || (category == "tradingcards" && IsSportsValue(subcategory)))
        {
            return CombineOptional(Path.Combine("cards", "sports"), ResolveSportsSegment(subcategory));
        }

        if (IsTcgCategory(category) || (category == "tradingcards" && IsTcgValue(subcategory)))
        {
            return CombineOptional(Path.Combine("cards", "tcg-ccg"), ResolveTcgSegment(subcategory));
        }

        if (category == "tradingcards")
        {
            return Path.Combine("cards", "trading-cards");
        }

        if (category is "stamps" or "stamp")
        {
            return CombineOptional("stamps", ResolveStampCountrySegment(subcategory, tags));
        }

        return category switch
        {
            "books" or "book" => "books",
            "comics" or "comic" or "comicbooks" or "comicbook" => "comics",
            "fastfoodtoys" or "fastfoodtoy" => Path.Combine("toys", "fast-food-toys"),
            "memorabilia" => "memorabilia",
            "newspapers" or "newspaper" => "newspapers",
            "other" => "other",
            "photos" or "photo" or "photographs" or "photograph" => "photos",
            "postcards" or "postcard" => "postcards",
            "toys" or "toy" => "toys",
            _ => string.Empty
        };
    }

    private static bool IsSportsCategory(string category) =>
        category is "sportscards" or "sportscard" or "sports cards" or "sports card";

    private static bool IsTcgCategory(string category) =>
        category is "tcgccg" or "tcg" or "ccg" or "collectiblecardgames" or "collectiblecardgame" or "tradingcardgames" or "tradingcardgame";

    private static string ResolveSportsSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return IsSportsValue(value) || !IsTcgValue(value) ? ToDirectorySegment(value) : string.Empty;
    }

    private static bool IsSportsValue(string value)
    {
        var normalized = NormalizeKey(value);
        return SportsCardSports.Any(sport => NormalizeKey(sport) == normalized);
    }

    private static string ResolveTcgSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return TcgAliases.TryGetValue(NormalizeKey(value), out var canonical)
            ? ToDirectorySegment(canonical)
            : ToDirectorySegment(value);
    }

    private static bool IsTcgValue(string value) =>
        !string.IsNullOrWhiteSpace(value) && TcgAliases.ContainsKey(NormalizeKey(value));

    private static string ResolveStampCountrySegment(string subcategory, IReadOnlyList<string> tags)
    {
        if (TryResolveCountry(subcategory, out var country))
        {
            return ToDirectorySegment(country);
        }

        foreach (var tag in tags)
        {
            if (TryResolveCountry(tag, out country))
            {
                return ToDirectorySegment(country);
            }
        }

        return string.Empty;
    }

    private static bool TryResolveCountry(string value, out string country)
    {
        country = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!CountryAliases.TryGetValue(NormalizeKey(value), out var resolved))
        {
            return false;
        }

        country = resolved;
        return true;
    }

    private static bool ShouldCopyToEbayTemp(InventoryItem item, IReadOnlyList<string> tags)
    {
        if (IsListedStatus(item.ListingStatus) || HasAnyTag(tags, "listed", "currently listed", "currentlylisted", "active listing", "sold"))
        {
            return false;
        }

        return item.ListingStatus is ListingStatus.ReadyToList or ListingStatus.Ready or ListingStatus.Draft or ListingStatus.Drafted
            || HasAnyTag(tags, "to be listed", "tobelisted", "to-be-listed", "ready to list", "list on ebay", "ebay");
    }

    private static bool IsListedStatus(ListingStatus status) =>
        status is ListingStatus.Active
            or ListingStatus.CurrentlyListed
            or ListingStatus.Listed
            or ListingStatus.Sold
            or ListingStatus.Ended
            or ListingStatus.Archived
            or ListingStatus.Cancelled;

    private static bool HasAnyTag(IReadOnlyList<string> tags, params string[] values)
    {
        var normalizedTags = tags.Select(NormalizeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return values.Select(NormalizeKey).Any(normalizedTags.Contains);
    }

    private static string CombineOptional(string root, string child) =>
        string.IsNullOrWhiteSpace(child) ? root : Path.Combine(root, child);

    private static string NormalizeKey(string value) =>
        new(value
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .Where(c => c != ' ')
            .ToArray());

    private static IReadOnlyDictionary<string, string> BuildAliasMap(IEnumerable<string> canonicalValues, Dictionary<string, string> extras)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in canonicalValues)
        {
            map[NormalizeKey(value)] = value;
        }

        foreach (var (alias, canonical) in extras)
        {
            map[NormalizeKey(alias)] = canonical;
        }

        return map;
    }
}

