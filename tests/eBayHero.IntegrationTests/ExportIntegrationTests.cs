using eBayHero.Core.Models;
using eBayHero.Core.Services;
using eBayHero.Export;
using Microsoft.Extensions.Logging.Abstractions;

namespace eBayHero.IntegrationTests;

public sealed class ExportIntegrationTests
{
    [Fact]
    public async Task EbayExport_CopiesImagesAndWritesManifests()
    {
        var root = Path.Combine(Path.GetTempPath(), "ipo-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "source");
            var export = Path.Combine(root, "export");
            Directory.CreateDirectory(source);
            var front = Path.Combine(source, "front.jpg");
            var back = Path.Combine(source, "back.jpg");
            await File.WriteAllTextAsync(front, "front");
            await File.WriteAllTextAsync(back, "back");

            var item = new InventoryItem { Id = "item-1", Name = "Test Card", Category = "Trading Cards" };
            var photos = new[]
            {
                new Photo { Id = "back", FullPath = back, Extension = ".jpg", ViewType = PhotoViewType.Back },
                new Photo { Id = "front", FullPath = front, Extension = ".jpg", ViewType = PhotoViewType.Front }
            };
            var service = new EbayExportService(NullLogger<EbayExportService>.Instance);

            var result = await service.ExportAsync(
                new ExportRequest(export, [item], new Dictionary<string, IReadOnlyList<Photo>> { [item.Id] = photos }, ListingStatus.Drafted),
                CancellationToken.None);

            Assert.True(File.Exists(result.CsvManifestPath));
            Assert.True(File.Exists(result.JsonManifestPath));
            Assert.True(File.Exists(result.ReadmePath));
            Assert.Equal(2, Directory.GetFiles(result.ExportDirectory, "*.jpg").Length);
            var csv = await File.ReadAllTextAsync(result.CsvManifestPath);
            Assert.Contains("front", csv);
            Assert.Contains("back", csv);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}


