namespace eBayHero.App.ViewModels;

public sealed class WorkspaceRow
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class OcrCandidateRow
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double Score { get; set; }
    public string Profile { get; set; } = string.Empty;
    public string DerivedImagePath { get; set; } = string.Empty;
}

