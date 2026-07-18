namespace eBayHero.Core.Configuration;

public sealed class InventoryOptions
{
    public string OperationsRoot { get; set; } = @"D:\INVENTORY_PHOTO_OPS";
    public string DefaultSourceRoot { get; set; } = @"F:\Inventory";
    public string TesseractPath { get; set; } = @"E:\Apps\tesseract-ocr\tesseract.exe";
    public string OcrLanguage { get; set; } = "eng";
    public int OcrMinimumConfidence { get; set; } = 38;
    public int OcrUpscaleFactor { get; set; } = 3;
    public int ThumbnailSize { get; set; } = 256;
    public int AutoGroupSeconds { get; set; } = 90;
    public int MaxAutomaticGroupSize { get; set; } = 12;
    public string DefaultListingStatus { get; set; } = "NotListed";
    public string EbayExportRoot => Path.Combine(OperationsRoot, "ebay-temp");
    public string DatabasePath => Path.Combine(OperationsRoot, "db", "ebay-hero.sqlite");
    public string LegacyJsonPath => Path.Combine(OperationsRoot, "db", "inventory-index.json");
    public string ThumbnailCacheRoot => Path.Combine(OperationsRoot, "cache", "thumbnails");
    public string OcrTrainingRoot => Path.Combine(OperationsRoot, "ocr-training");
    public string OcrTempRoot => Path.Combine(OcrTrainingRoot, "temp");
    public string OcrSamplesRoot => Path.Combine(OcrTrainingRoot, "samples");
    public string OcrLearningPath => Path.Combine(OcrTrainingRoot, "ocr-learning.json");
    public string OcrUserWordsPath => Path.Combine(OcrTrainingRoot, "tesseract-user-words.txt");
    public bool DevelopmentSafeMode { get; set; } = true;
}


