using eBayHero.Core.Models;

namespace eBayHero.App.ViewModels;

public sealed class PhotoGridRow
{
    public string PhotoId { get; init; } = string.Empty;
    public string ItemId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string ImageRoleCode { get; init; } = string.Empty;
    public string ViewType { get; init; } = string.Empty;
    public ListingStatus ListingStatus { get; init; }
    public string Category { get; init; } = string.Empty;
    public string SportOrGame { get; init; } = string.Empty;
    public string PlayerOrTitle { get; init; } = string.Empty;
    public string Year { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string SetName { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string CardNumber { get; init; } = string.Empty;
    public string Condition { get; init; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public double OcrConfidence { get; init; }
    public bool OcrReviewed { get; init; }
    public string FullPath { get; init; } = string.Empty;
    public bool IsMissing { get; init; }
    public bool IsDuplicate { get; init; }
}

