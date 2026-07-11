using System.IO;

namespace InventoryPhotoOps.UiTests;

public sealed class MainWindowXamlTests
{
    [Fact]
    public async Task MainGrid_HasVisibleHeadersAndVirtualization()
    {
        var xamlPath = Path.Combine(AppContext.BaseDirectory, "MainWindow.xaml");
        var xaml = await File.ReadAllTextAsync(xamlPath);

        Assert.Contains("HeadersVisibility=\"Column\"", xaml);
        Assert.Contains("EnableRowVirtualization=\"True\"", xaml);
        Assert.Contains("VirtualizationMode=\"Recycling\"", xaml);
        Assert.Contains("SelectionMode=\"Extended\"", xaml);
        Assert.Contains("Apply metadata edits", xaml);
        Assert.Contains("MetadataItemName", xaml);
        Assert.Contains("Apply to selected", xaml);
        Assert.Contains("Hide currently listed", xaml);
        Assert.Contains("HideHiddenMetadataTerms", xaml);
        Assert.Contains("UseWorkflowSort", xaml);
        Assert.Contains("Dedupe scan", xaml);
        Assert.Contains("OCR highlighted", xaml);
        Assert.Contains("Auto sort categorized", xaml);
        Assert.Contains("OcrSelectionCanvas", xaml);
        Assert.Contains("OcrRegionSummary", xaml);
        Assert.Contains("CategoryOptions", xaml);
        Assert.Contains("SportOrGameOptions", xaml);
        Assert.Contains("BrandOptions", xaml);
        Assert.Contains("MetadataViewFront", xaml);
        Assert.Contains("Straighten", xaml);
    }
}
