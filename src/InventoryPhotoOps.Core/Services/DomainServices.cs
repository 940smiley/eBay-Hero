using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using InventoryPhotoOps.Core.Models;

namespace InventoryPhotoOps.Core.Services;

public static partial class PathUtility
{
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

public sealed class WindowsPathService : IPathService
{
    public string NormalizePath(string path) => PathUtility.NormalizePath(path);

    public bool IsSamePath(string left, string right) =>
        string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    public bool IsChildOf(string childPath, string parentPath)
    {
        var child = NormalizePath(childPath);
        var parent = NormalizePath(parentPath);
        return child.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}

public static class FilenameSanitizer
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5",
        "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4",
        "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string Sanitize(string value, int maxLength = 120, string fallback = "item")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            builder.Append(invalid.Contains(c) || char.IsControl(c) ? ' ' : c);
        }

        var cleaned = Regex.Replace(builder.ToString(), @"\s+", " ").Trim(' ', '.');
        if (cleaned.Length > maxLength)
        {
            cleaned = cleaned[..maxLength].Trim(' ', '.');
        }

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = fallback;
        }

        if (ReservedNames.Contains(cleaned))
        {
            cleaned = "_" + cleaned;
        }

        return cleaned;
    }
}

public static class FilenameTemplateRenderer
{
    public static string Render(string template, InventoryItem item, Photo photo, int sequence)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Player"] = item.PlayerOrTitle,
            ["PlayerOrTitle"] = item.PlayerOrTitle,
            ["Title"] = item.PlayerOrTitle,
            ["CardNumber"] = item.CardNumber,
            ["Year"] = item.Year,
            ["Brand"] = item.Brand,
            ["Set"] = item.SetName,
            ["SetName"] = item.SetName,
            ["SerialNumber"] = item.SerialNumber,
            ["Author"] = item.Author,
            ["ISBN"] = item.ISBN,
            ["ItemName"] = item.Name,
            ["View"] = photo.ViewType.ToString(),
            ["Sequence"] = sequence.ToString("D2", CultureInfo.InvariantCulture)
        };

        var rendered = Regex.Replace(template, @"\{(?<key>[A-Za-z0-9_]+)\}", match =>
        {
            var key = match.Groups["key"].Value;
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        });

        return FilenameSanitizer.Sanitize(rendered);
    }
}

public static partial class MetadataExtractor
{
    private static readonly string[] Brands =
    [
        "Topps Chrome", "Bowman Chrome", "Topps Heritage", "Topps Finest", "Topps Archives",
        "Allen & Ginter", "Gypsy Queen", "National Treasures", "Upper Deck",
        "O-Pee-Chee", "Stadium Club", "Panini", "Donruss", "Fleer", "Score",
        "Leaf", "Pinnacle", "Select", "Prizm", "Mosaic", "Optic", "Hoops",
        "SkyBox", "Bowman", "Topps", "Chrome", "Contenders", "Chronicles"
    ];

    public static string ExtractYear(string text)
    {
        var match = YearRegex().Match(text);
        return match.Success ? match.Value : string.Empty;
    }

    public static string ExtractSerialNumber(string text)
    {
        var match = SerialRegex().Match(text);
        return match.Success ? Regex.Replace(match.Value, @"\s+", "") : string.Empty;
    }

    public static string ExtractCardNumber(string text)
    {
        var match = CardNumberRegex().Match(text);
        return match.Success ? match.Groups["number"].Value.Trim() : string.Empty;
    }

    public static string ExtractBrand(string text)
    {
        foreach (var brand in Brands.OrderByDescending(b => b.Length))
        {
            if (text.Contains(brand, StringComparison.OrdinalIgnoreCase))
            {
                return brand;
            }
        }

        return string.Empty;
    }

    [GeneratedRegex(@"\b(19[5-9]\d|20[0-3]\d)\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\b\d{1,6}\s*(?:/|of)\s*\d{1,6}\b", RegexOptions.IgnoreCase)]
    private static partial Regex SerialRegex();

    [GeneratedRegex(@"(?:card\s*)?(?:no\.?|#)\s*(?<number>[A-Za-z0-9\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CardNumberRegex();
}

public static class OcrCandidateScorer
{
    public static double Score(string text, double confidence, int psm)
    {
        var normalized = Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
        if (normalized.Length == 0)
        {
            return -1000;
        }

        var length = normalized.Length;
        var letters = normalized.Count(char.IsLetter);
        var digits = normalized.Count(char.IsDigit);
        var allowed = normalized.Count(c => char.IsLetterOrDigit(c) || " .,'&#/+():;-".Contains(c));
        var letterRatio = letters / (double)length;
        var allowedRatio = allowed / (double)length;
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var singleLetters = tokens.Count(t => t.Length == 1 && char.IsLetter(t[0]));

        var score = confidence;
        if (length is >= 3 and <= 120) score += 8;
        if (letterRatio >= 0.45 || digits > 0) score += 10;
        if (allowedRatio >= 0.90) score += 8;
        if (MetadataExtractor.ExtractYear(normalized).Length > 0) score += 4;
        if (MetadataExtractor.ExtractSerialNumber(normalized).Length > 0) score += 8;
        if (psm is 7 or 13 && length <= 70) score += 5;
        if (tokens.Length > 2 && singleLetters >= Math.Ceiling(tokens.Length / 2.0)) score -= 30;
        if (normalized.Count(char.IsPunctuation) > length * 0.45) score -= 25;
        if (normalized.Any(c => char.IsSurrogate(c) || char.GetUnicodeCategory(c) == UnicodeCategory.OtherNotAssigned)) score -= 25;
        return Math.Round(score, 2);
    }
}

public static partial class CardMetadataAnalyzer
{
    private const int EbayTitleLimit = 80;

    private static readonly string[] Manufacturers =
    [
        "Topps Chrome", "Bowman Chrome", "Upper Deck", "O-Pee-Chee",
        "Topps", "Panini", "Donruss", "Bowman", "Fleer", "Score",
        "Leaf", "SkyBox", "Hoops", "Prizm", "Select", "Mosaic", "Optic"
    ];

    private static readonly Dictionary<string, string[]> SportHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Baseball"] = ["baseball", "mlb"],
        ["Basketball"] = ["basketball", "nba"],
        ["Football"] = ["football", "nfl"],
        ["Hockey"] = ["hockey", "nhl"],
        ["Soccer"] = ["soccer", "futbol", "football club", "fc "],
        ["Racing"] = ["nascar", "racing"],
        ["Wrestling"] = ["wrestling", "wwe", "aew"]
    };

    private static readonly HashSet<string> PlayerBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "rookie", "rc", "baseball", "basketball", "football", "hockey", "soccer",
        "topps", "panini", "donruss", "bowman", "upper", "deck", "prizm",
        "chrome", "select", "mosaic", "optic", "front", "back", "obverse", "reverse"
    };

    public static CardMetadataAnalysis AnalyzeText(
        string text,
        string sourceIdentifier,
        double ocrConfidence,
        double confidenceThreshold = 38)
    {
        var normalized = CleanSpace(text);
        var lines = Regex.Split(text ?? string.Empty, @"[\r\n|]+")
            .Select(CleanSpace)
            .Where(line => line.Length > 0)
            .ToList();
        var evidence = new List<CardFieldEvidence>();
        var candidate = new InventoryItem { Category = "Trading Cards" };

        void SetField(string fieldName, string value, double confidence, Action<InventoryItem, string> setter, string source = "local_heuristic")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            setter(candidate, value);
            evidence.Add(new CardFieldEvidence(fieldName, value, source, sourceIdentifier, Math.Round(confidence, 3)));
        }

        SetField(nameof(InventoryItem.Year), MetadataExtractor.ExtractYear(normalized), Weighted(ocrConfidence, 0.85), static (i, v) => i.Year = v);

        var sport = DetectSport(normalized);
        SetField(nameof(InventoryItem.SportOrGame), sport.Value, sport.Confidence, static (i, v) => i.SportOrGame = v);

        var manufacturer = DetectManufacturer(normalized);
        SetField(nameof(InventoryItem.Manufacturer), manufacturer.Value, manufacturer.Confidence, static (i, v) => i.Manufacturer = v);
        SetField(nameof(InventoryItem.Brand), manufacturer.Value, Math.Min(manufacturer.Confidence, 76), static (i, v) => i.Brand = v);

        var cardNumber = MetadataExtractor.ExtractCardNumber(normalized);
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            cardNumber = FirstMatch(normalized, @"\bcard\s+(?<value>[A-Z0-9-]{1,12})\b");
        }

        SetField(nameof(InventoryItem.CardNumber), cardNumber, Weighted(ocrConfidence, 0.72), static (i, v) => i.CardNumber = v);
        SetField(nameof(InventoryItem.SerialNumber), MetadataExtractor.ExtractSerialNumber(normalized), Weighted(ocrConfidence, 0.90), static (i, v) => i.SerialNumber = v);

        if (RookieRegex().IsMatch(normalized))
        {
            candidate.Rookie = true;
            evidence.Add(new CardFieldEvidence(nameof(InventoryItem.Rookie), "true", "local_heuristic", sourceIdentifier, 82));
        }

        if (AutographRegex().IsMatch(normalized))
        {
            candidate.Autograph = true;
            evidence.Add(new CardFieldEvidence(nameof(InventoryItem.Autograph), "true", "local_heuristic", sourceIdentifier, 78));
        }

        if (RelicRegex().IsMatch(normalized))
        {
            candidate.Relic = true;
            evidence.Add(new CardFieldEvidence(nameof(InventoryItem.Relic), "true", "local_heuristic", sourceIdentifier, 76));
        }

        var grade = GradeRegex().Match(normalized);
        if (grade.Success)
        {
            SetField(nameof(InventoryItem.GradingCompany), grade.Groups["company"].Value.ToUpperInvariant(), 90, static (i, v) => i.GradingCompany = v);
            SetField(nameof(InventoryItem.Grade), grade.Groups["grade"].Value, 90, static (i, v) => i.Grade = v);
        }

        var player = DetectPlayer(lines.Count > 0 ? lines : [normalized]);
        SetField(nameof(InventoryItem.PlayerOrTitle), player.Value, player.Confidence, static (i, v) => i.PlayerOrTitle = v);

        candidate.Name = BuildItemName(candidate);

        var confidence = evidence.Count == 0
            ? 0
            : Math.Min(98, evidence.Average(e => e.Confidence));
        if (evidence.Count > 0)
        {
            confidence = Math.Max(confidence, Math.Min(72, ocrConfidence * 0.86));
        }

        var unresolved = new[]
        {
            nameof(InventoryItem.SportOrGame),
            nameof(InventoryItem.Year),
            nameof(InventoryItem.Brand),
            nameof(InventoryItem.PlayerOrTitle),
            nameof(InventoryItem.SetName),
            nameof(InventoryItem.CardNumber)
        }.Where(field => IsUnresolved(candidate, field)).ToList();

        var status = confidence >= confidenceThreshold && unresolved.Count <= 2 ? "ready_for_review" : "needs_review";
        return new CardMetadataAnalysis(candidate, Math.Round(confidence, 2), status, unresolved, evidence, normalized);
    }

    public static ListingRecommendation RecommendListing(
        InventoryItem item,
        decimal? manualValue = null,
        string defaultListingFormat = "fixed_price",
        double confidence = 0,
        double confidenceThreshold = 72)
    {
        var title = BuildEbayTitle(item);
        var warnings = title.Warnings.ToList();
        var incomplete = string.IsNullOrWhiteSpace(item.PlayerOrTitle) || string.IsNullOrWhiteSpace(item.Year) || string.IsNullOrWhiteSpace(item.CardNumber);
        var lowConfidence = confidence > 0 && confidence < confidenceThreshold;
        var recommendedPrice = manualValue ?? item.SalePrice;
        var pricingSource = recommendedPrice.HasValue ? "ManualValue" : "not_configured";
        var listingFormat = incomplete || lowConfidence
            ? "needs_review"
            : recommendedPrice.HasValue && recommendedPrice.Value < 8m
                ? "auction_or_lot"
                : defaultListingFormat;
        var lotAssignment = listingFormat switch
        {
            "needs_review" => "identity-review",
            "auction_or_lot" => "low-value-lot",
            _ => "single-card"
        };

        if (lowConfidence)
        {
            warnings.Add($"Card confidence is below the configured {confidenceThreshold:0.#}% threshold.");
        }

        if (incomplete)
        {
            warnings.Add("Title has incomplete card identity fields.");
        }

        return new ListingRecommendation(
            title.Title,
            title.Title.Length,
            EbayTitleLimit,
            warnings,
            listingFormat,
            recommendedPrice,
            pricingSource,
            recommendedPrice.HasValue,
            recommendedPrice.HasValue ? null : "No configured pricing source and no manual value.",
            lotAssignment,
            "Deterministic local card recommendation");
    }

    public static (string Title, IReadOnlyList<string> Warnings) BuildEbayTitle(InventoryItem item)
    {
        var parts = new[]
        {
            item.Year,
            FirstNonEmpty(item.Brand, item.Manufacturer),
            item.SetName,
            item.PlayerOrTitle,
            string.IsNullOrWhiteSpace(item.CardNumber) ? string.Empty : "#" + item.CardNumber,
            item.Rookie ? "RC" : string.Empty,
            item.Autograph ? "Auto" : string.Empty,
            item.Relic ? "Relic" : string.Empty,
            string.IsNullOrWhiteSpace(item.GradingCompany) || string.IsNullOrWhiteSpace(item.Grade)
                ? string.Empty
                : $"{item.GradingCompany} {item.Grade}"
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        var warnings = new List<string>();
        var title = CleanSpace(string.Join(' ', parts));
        if (string.IsNullOrWhiteSpace(title))
        {
            title = string.IsNullOrWhiteSpace(item.Name) ? $"Sports Card {item.Id}" : item.Name;
            warnings.Add("Title has insufficient card identity fields.");
        }

        if (title.Length > EbayTitleLimit)
        {
            title = title[..EbayTitleLimit].Trim();
            warnings.Add("Title was trimmed to the 80-character marketplace limit.");
        }

        return (title, warnings);
    }

    private static string BuildItemName(InventoryItem item)
    {
        var title = BuildEbayTitle(item).Title;
        return title.StartsWith("Sports Card ", StringComparison.OrdinalIgnoreCase) ? string.Empty : title;
    }

    private static (string Value, double Confidence) DetectSport(string text)
    {
        foreach (var (sport, hints) in SportHints)
        {
            if (hints.Any(hint => text.Contains(hint, StringComparison.OrdinalIgnoreCase)))
            {
                return (sport, 80);
            }
        }

        return (string.Empty, 0);
    }

    private static (string Value, double Confidence) DetectManufacturer(string text)
    {
        foreach (var manufacturer in Manufacturers.OrderByDescending(m => m.Length))
        {
            if (text.Contains(manufacturer, StringComparison.OrdinalIgnoreCase))
            {
                return (manufacturer, 86);
            }
        }

        return (string.Empty, 0);
    }

    private static (string Value, double Confidence) DetectPlayer(IReadOnlyList<string> lines)
    {
        foreach (var raw in lines)
        {
            var cleaned = Regex.Replace(raw, @"[^A-Za-z .'-]", " ");
            var words = CleanSpace(cleaned)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(word => word.Length > 1 && !PlayerBlacklist.Contains(word))
                .ToList();

            if (words.Count is < 2 or > 4)
            {
                continue;
            }

            var candidate = CleanSpace(string.Join(' ', words));
            if (candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(PlayerBlacklist.Contains))
            {
                continue;
            }

            return (TitleCase(candidate), 58);
        }

        return (string.Empty, 0);
    }

    private static string TitleCase(string value)
    {
        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        return string.Join(' ', CleanSpace(value).Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word =>
            word.All(char.IsUpper) && word.Length <= 4 ? word : textInfo.ToTitleCase(word.ToLowerInvariant())));
    }

    private static bool IsUnresolved(InventoryItem item, string field) => field switch
    {
        nameof(InventoryItem.SportOrGame) => string.IsNullOrWhiteSpace(item.SportOrGame),
        nameof(InventoryItem.Year) => string.IsNullOrWhiteSpace(item.Year),
        nameof(InventoryItem.Brand) => string.IsNullOrWhiteSpace(item.Brand),
        nameof(InventoryItem.PlayerOrTitle) => string.IsNullOrWhiteSpace(item.PlayerOrTitle),
        nameof(InventoryItem.SetName) => string.IsNullOrWhiteSpace(item.SetName),
        nameof(InventoryItem.CardNumber) => string.IsNullOrWhiteSpace(item.CardNumber),
        _ => true
    };

    private static string FirstMatch(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
    }

    private static double Weighted(double ocrConfidence, double factor) => Math.Clamp(ocrConfidence * factor, 0, 95);

    private static string CleanSpace(string? value) => Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();

    private static string FirstNonEmpty(params string[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    [GeneratedRegex(@"\b(RC|rookie)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RookieRegex();

    [GeneratedRegex(@"\b(auto|autograph|signed)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AutographRegex();

    [GeneratedRegex(@"\b(relic|patch|jersey|memorabilia)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RelicRegex();

    [GeneratedRegex(@"\b(?<company>PSA|BGS|SGC|CGC)\s*(?<grade>\d+(?:\.\d)?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GradeRegex();
}

public static class PhotoPairingAnalyzer
{
    public static PhotoViewType InferViewTypeFromFileName(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        if (Regex.IsMatch(stem, @"(^|[_\-\s])(front|obverse|frnt|f)([_\-\s]|$)"))
        {
            return PhotoViewType.Front;
        }

        if (Regex.IsMatch(stem, @"(^|[_\-\s])(back|reverse|rear|b)([_\-\s]|$)"))
        {
            return PhotoViewType.Back;
        }

        if (stem.Contains("serial", StringComparison.OrdinalIgnoreCase))
        {
            return PhotoViewType.SerialNumber;
        }

        if (stem.Contains("cardnumber", StringComparison.OrdinalIgnoreCase) || stem.Contains("card-number", StringComparison.OrdinalIgnoreCase))
        {
            return PhotoViewType.CardNumber;
        }

        return PhotoViewType.Unknown;
    }

    public static IReadOnlyList<FrontBackPairCandidate> ProposeFrontBackPairs(IEnumerable<Photo> photos)
    {
        var buckets = new Dictionary<string, Dictionary<PhotoViewType, Photo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var photo in photos)
        {
            var inferred = photo.ViewType == PhotoViewType.Unknown ? InferViewTypeFromFileName(photo.FileName) : photo.ViewType;
            if (inferred is not (PhotoViewType.Front or PhotoViewType.Back))
            {
                continue;
            }

            var key = BasePairKey(photo.FileName);
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new Dictionary<PhotoViewType, Photo>();
                buckets[key] = bucket;
            }

            bucket.TryAdd(inferred, photo);
        }

        return buckets.Values
            .Where(bucket => bucket.ContainsKey(PhotoViewType.Front) && bucket.ContainsKey(PhotoViewType.Back))
            .Select(bucket => new FrontBackPairCandidate(
                bucket[PhotoViewType.Front].Id,
                bucket[PhotoViewType.Back].Id,
                0.78,
                "filename front/back pattern"))
            .ToList();
    }

    private static string BasePairKey(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        stem = Regex.Replace(stem, @"(^|[_\-\s])(front|obverse|frnt|back|reverse|rear)([_\-\s]|$)", "$1$3");
        stem = Regex.Replace(stem, @"([_\-\s])([fb])$", string.Empty);
        return Regex.Replace(stem, @"[_\-\s]+", " ").Trim();
    }
}

public static partial class ImageRoleCodeGenerator
{
    public static IReadOnlyList<string> GenerateNormalSeries(char prefix, int count)
    {
        var normalized = char.ToUpperInvariant(prefix);
        if (normalized is < 'A' or > 'Z')
        {
            throw new ArgumentOutOfRangeException(nameof(prefix), "Image role prefix must be A through Z.");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return Enumerable.Range(0, count)
            .Select(index => index == 0 ? normalized.ToString() : normalized + index.ToString(CultureInfo.InvariantCulture))
            .ToList();
    }

    public static IReadOnlyList<string> GenerateCornerSeries(int count)
    {
        if (count is < 0 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Corner shot labels support CS1 through CS8.");
        }

        return Enumerable.Range(1, count).Select(index => "CS" + index.ToString(CultureInfo.InvariantCulture)).ToList();
    }

    public static IReadOnlyList<string> GenerateDamageSeries(int count)
    {
        if (count is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Damage documentation labels support Dd1 through Dd10.");
        }

        return Enumerable.Range(1, count).Select(index => "Dd" + index.ToString(CultureInfo.InvariantCulture)).ToList();
    }

    public static char NextAdditionalPrefix(IEnumerable<string> existingCodes)
    {
        var used = existingCodes
            .Select(NormalizeOrEmpty)
            .Where(code => NormalCodeRegex().IsMatch(code))
            .Select(code => code[0])
            .Where(prefix => prefix is >= 'C' and <= 'Z')
            .ToHashSet();

        for (var prefix = 'C'; prefix <= 'Z'; prefix++)
        {
            if (!used.Contains(prefix))
            {
                return prefix;
            }
        }

        throw new InvalidOperationException("All additional image prefixes C through Z are already used for this item.");
    }

    public static bool IsValid(string code) => !string.IsNullOrWhiteSpace(NormalizeOrEmpty(code));

    public static string Normalize(string code)
    {
        var normalized = NormalizeOrEmpty(code);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Image role code must be A-Z, CS1-CS8, or Dd1-Dd10.", nameof(code));
        }

        return normalized;
    }

    private static string NormalizeOrEmpty(string? code)
    {
        var trimmed = (code ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var upper = trimmed.ToUpperInvariant();
        if (NormalCodeRegex().IsMatch(upper))
        {
            return upper;
        }

        var corner = CornerCodeRegex().Match(upper);
        if (corner.Success)
        {
            var value = int.Parse(corner.Groups["number"].Value, CultureInfo.InvariantCulture);
            return value is >= 1 and <= 8 ? "CS" + value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        var damage = DamageCodeRegex().Match(upper);
        if (damage.Success)
        {
            var value = int.Parse(damage.Groups["number"].Value, CultureInfo.InvariantCulture);
            return value is >= 1 and <= 10 ? "Dd" + value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        return string.Empty;
    }

    [GeneratedRegex(@"^[A-Z](?:[1-9]\d*)?$")]
    private static partial Regex NormalCodeRegex();

    [GeneratedRegex(@"^CS(?<number>\d+)$")]
    private static partial Regex CornerCodeRegex();

    [GeneratedRegex(@"^DD(?<number>\d+)$")]
    private static partial Regex DamageCodeRegex();
}

public sealed class ExportPhotoOrderComparer : IComparer<Photo>
{
    public int Compare(Photo? x, Photo? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var order = GetOrder(x.ViewType).CompareTo(GetOrder(y.ViewType));
        return order != 0
            ? order
            : string.Compare(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase);
    }

    public static int GetOrder(PhotoViewType viewType) => viewType switch
    {
        PhotoViewType.Front => 10,
        PhotoViewType.Back => 20,
        PhotoViewType.Top or PhotoViewType.Bottom or PhotoViewType.Left or PhotoViewType.Right => 30,
        PhotoViewType.FrontUpperLeft or PhotoViewType.FrontUpperRight or PhotoViewType.FrontLowerLeft or PhotoViewType.FrontLowerRight => 40,
        PhotoViewType.BackUpperLeft or PhotoViewType.BackUpperRight or PhotoViewType.BackLowerLeft or PhotoViewType.BackLowerRight => 50,
        PhotoViewType.SerialNumber or PhotoViewType.CardNumber => 60,
        PhotoViewType.ConditionCloseup => 70,
        _ => 100
    };
}
