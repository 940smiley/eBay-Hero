using eBayHero.Core.Models;
using eBayHero.Core.Services;

namespace eBayHero.Core.Tests;

public sealed class DomainServiceTests
{
    [Fact]
    public void FilenameSanitizer_RemovesInvalidCharactersAndReservedNames()
    {
        Assert.Equal("_CON", FilenameSanitizer.Sanitize("CON"));
        Assert.Equal("Ruben Amaro 12", FilenameSanitizer.Sanitize("Ruben<Amaro>:12"));
    }

    [Fact]
    public void MetadataExtractor_FindsSportsCardFields()
    {
        var text = "2023 Topps Chrome RUBEN AMARO Card #12 23/99";

        Assert.Equal("2023", MetadataExtractor.ExtractYear(text));
        Assert.Equal("Topps Chrome", MetadataExtractor.ExtractBrand(text));
        Assert.Equal("12", MetadataExtractor.ExtractCardNumber(text));
        Assert.Equal("23/99", MetadataExtractor.ExtractSerialNumber(text));
    }

    [Fact]
    public void OcrCandidateScorer_PenalizesNoise()
    {
        var good = OcrCandidateScorer.Score("Ruben Amaro 2023 Topps Chrome #12", 74, 7);
        var noise = OcrCandidateScorer.Score("A B C ! ! ! | | |", 15, 11);

        Assert.True(good > noise);
    }

    [Fact]
    public void ExportOrderComparer_PutsFrontBeforeBackAndDetails()
    {
        var photos = new[]
        {
            new Photo { FullPath = "detail.jpg", ViewType = PhotoViewType.ConditionCloseup },
            new Photo { FullPath = "back.jpg", ViewType = PhotoViewType.Back },
            new Photo { FullPath = "front.jpg", ViewType = PhotoViewType.Front }
        };

        var ordered = photos.Order(new ExportPhotoOrderComparer()).Select(p => p.ViewType).ToArray();

        Assert.Equal([PhotoViewType.Front, PhotoViewType.Back, PhotoViewType.ConditionCloseup], ordered);
    }

    [Fact]
    public void FilenameTemplateRenderer_RendersGroupTemplate()
    {
        var item = new InventoryItem
        {
            PlayerOrTitle = "Ruben Amaro",
            CardNumber = "12",
            Year = "1956",
            Brand = "Topps",
            SetName = "Baseball"
        };
        var photo = new Photo { ViewType = PhotoViewType.Front };

        var rendered = FilenameTemplateRenderer.Render("{Player}_{CardNumber}_{Year}_{Brand}_{Set}_{View}_{Sequence}", item, photo, 1);

        Assert.Equal("Ruben Amaro_12_1956_Topps_Baseball_Front_01", rendered);
    }

    [Fact]
    public void CardMetadataAnalyzer_ExtractsCardEvidenceAndBuildsTitle()
    {
        var text = """
        Baseball
        2023 Topps Chrome
        RUBEN AMARO
        Card #12
        23/99 RC Auto PSA 9
        """;

        var analysis = CardMetadataAnalyzer.AnalyzeText(text, "photo-1", 82);
        var title = CardMetadataAnalyzer.BuildEbayTitle(analysis.Candidate);

        Assert.Equal("Trading Cards", analysis.Candidate.Category);
        Assert.Equal("Baseball", analysis.Candidate.SportOrGame);
        Assert.Equal("2023", analysis.Candidate.Year);
        Assert.Equal("Topps Chrome", analysis.Candidate.Brand);
        Assert.Equal("12", analysis.Candidate.CardNumber);
        Assert.Equal("23/99", analysis.Candidate.SerialNumber);
        Assert.True(analysis.Candidate.Rookie);
        Assert.True(analysis.Candidate.Autograph);
        Assert.Equal("PSA", analysis.Candidate.GradingCompany);
        Assert.Equal("9", analysis.Candidate.Grade);
        Assert.Contains("#12", title.Title);
        Assert.True(title.Title.Length <= 80);
        Assert.Contains(analysis.Evidence, e => e.FieldName == nameof(InventoryItem.CardNumber));
    }

    [Fact]
    public void CardMetadataAnalyzer_RecommendsReviewWhenIdentityIsIncomplete()
    {
        var item = new InventoryItem { Brand = "Topps", Year = "2022" };

        var recommendation = CardMetadataAnalyzer.RecommendListing(item, confidence: 20);

        Assert.Equal("needs_review", recommendation.RecommendedListingFormat);
        Assert.Contains(recommendation.Warnings, warning => warning.Contains("incomplete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PhotoPairingAnalyzer_PairsFrontAndBackFilenamePatterns()
    {
        var photos = new[]
        {
            new Photo { Id = "front", FileName = "ruben-amaro_front.jpg" },
            new Photo { Id = "back", FileName = "ruben-amaro_back.jpg" },
            new Photo { Id = "detail", FileName = "ruben-amaro_serial.jpg" }
        };

        var pairs = PhotoPairingAnalyzer.ProposeFrontBackPairs(photos);

        Assert.Single(pairs);
        Assert.Equal("front", pairs[0].FrontPhotoId);
        Assert.Equal("back", pairs[0].BackPhotoId);
        Assert.Equal(PhotoViewType.SerialNumber, PhotoPairingAnalyzer.InferViewTypeFromFileName("ruben-amaro_serial.jpg"));
    }

    [Fact]
    public void ImageRoleCodeGenerator_GeneratesNormalFrontBackAndAdditionalSeries()
    {
        Assert.Equal(["A", "A1", "A2"], ImageRoleCodeGenerator.GenerateNormalSeries('A', 3));
        Assert.Equal(["B", "B1"], ImageRoleCodeGenerator.GenerateNormalSeries('b', 2));
        Assert.Equal('D', ImageRoleCodeGenerator.NextAdditionalPrefix(["A", "B", "C", "C1"]));
    }

    [Fact]
    public void ImageRoleCodeGenerator_GeneratesAndNormalizesSpecialSeries()
    {
        Assert.Equal(["CS1", "CS2"], ImageRoleCodeGenerator.GenerateCornerSeries(2));
        Assert.Equal(["Dd1", "Dd2", "Dd3"], ImageRoleCodeGenerator.GenerateDamageSeries(3));
        Assert.Equal("Dd10", ImageRoleCodeGenerator.Normalize("dd10"));
        Assert.True(ImageRoleCodeGenerator.IsValid("CS8"));
        Assert.False(ImageRoleCodeGenerator.IsValid("CS9"));
        Assert.False(ImageRoleCodeGenerator.IsValid("Dd11"));
    }
}

