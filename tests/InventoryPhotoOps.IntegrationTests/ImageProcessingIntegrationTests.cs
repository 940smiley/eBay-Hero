#pragma warning disable CA1416
using System.Drawing;
using System.Drawing.Imaging;
using InventoryPhotoOps.Core.Models;
using InventoryPhotoOps.Core.Services;
using InventoryPhotoOps.Ocr;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryPhotoOps.IntegrationTests;

public sealed class ImageProcessingIntegrationTests
{
    [Fact]
    public async Task Preprocessing_CreatesDerivedImagesAndCropMetadata()
    {
        using var temp = new TempDirectory();
        var source = Path.Combine(temp.Path, "card.png");
        CreateSampleCard(source, 180, 260);
        var service = new ImagePreprocessingService(NullLogger<ImagePreprocessingService>.Instance);

        var result = await service.PrepareAsync(
            new ImagePreprocessRequest("photo-1", source, OcrProfile.TradingCardFront, Path.Combine(temp.Path, "ocr")),
            CancellationToken.None);

        Assert.Contains(result.Variants, v => v.Kind == OcrImageKind.Thresholded);
        Assert.All(result.Variants, v => Assert.True(File.Exists(v.DerivedPath), v.DerivedPath));
        Assert.All(result.Variants, v => Assert.Contains("Width", v.CropRectangleJson));
    }

    [Fact]
    public async Task ImageEdit_CropsAndRotatesWithoutOverwritingOriginal()
    {
        using var temp = new TempDirectory();
        var source = Path.Combine(temp.Path, "card.png");
        CreateSampleCard(source, 120, 180);
        var originalWrite = File.GetLastWriteTimeUtc(source);
        var service = new ImageEditService();

        var result = await service.ApplyAsync(
            new ImageEditRequest(
                "photo-2",
                source,
                Path.Combine(temp.Path, "edits"),
                [
                    new ImageEditCommand(ImageEditOperationKind.Crop, "{\"x\":10,\"y\":10,\"width\":80,\"height\":120}"),
                    new ImageEditCommand(ImageEditOperationKind.RotateRight)
                ]),
            CancellationToken.None);

        Assert.True(File.Exists(result.OutputPath));
        Assert.False(result.OriginalOverwritten);
        Assert.Equal(originalWrite, File.GetLastWriteTimeUtc(source));
        Assert.Equal(2, result.Operations.Count);
    }

    private static void CreateSampleCard(string path, int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var brush = new SolidBrush(Color.LightGray);
        graphics.FillRectangle(brush, 20, 20, width - 40, height - 40);
        using var pen = new Pen(Color.Black, 3);
        graphics.DrawRectangle(pen, 20, 20, width - 40, height - 40);
        using var font = new Font(FontFamily.GenericSansSerif, 16);
        graphics.DrawString("1990 TOPPS", font, Brushes.Black, 32, 60);
        graphics.DrawString("#12", font, Brushes.Black, 32, 95);
        bitmap.Save(path, ImageFormat.Png);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InventoryPhotoOpsTests", Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
