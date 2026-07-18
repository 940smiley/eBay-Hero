#pragma warning disable CA1416
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using Microsoft.Extensions.Logging;

namespace eBayHero.Ocr;

public sealed class ImagePreprocessingService(ILogger<ImagePreprocessingService> logger) : IImagePreprocessingService
{
    public Task<ImagePreprocessResult> PrepareAsync(ImagePreprocessRequest request, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.SourcePath))
        {
            throw new FileNotFoundException("Image for preprocessing was not found.", request.SourcePath);
        }

        var warnings = new List<string>();
        var variants = new List<ImagePreprocessVariant>();
        var root = string.IsNullOrWhiteSpace(request.WorkingRoot)
            ? Path.Combine(Path.GetTempPath(), "eBayHero", "ocr")
            : request.WorkingRoot;
        var runRoot = Path.Combine(root, request.PhotoId, DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
        Directory.CreateDirectory(runRoot);

        using var original = new Bitmap(request.SourcePath);
        ApplyExifOrientation(original);
        var fullRect = new Rectangle(0, 0, original.Width, original.Height);
        variants.Add(SaveVariant(original, request.SourcePath, runRoot, OcrImageKind.Original, "original-auto-orient", fullRect, 0));

        using var rotated = RotateForProfile(original, request.Profile, out var rotation);
        var contentBounds = DetectContentBounds(rotated);
        if (contentBounds.Width < rotated.Width * 0.40 || contentBounds.Height < rotated.Height * 0.40)
        {
            warnings.Add("Card boundary detection was inconclusive; using the full image.");
            contentBounds = new Rectangle(0, 0, rotated.Width, rotated.Height);
        }

        var customCrop = TryParseCrop(request.CustomCropJson, rotated.Width, rotated.Height);
        var cropRects = new List<(string Name, Rectangle Rect)>
        {
            ("full", new Rectangle(0, 0, rotated.Width, rotated.Height)),
            ("card-boundary", contentBounds)
        };
        var objectIndex = 1;
        foreach (var objectBounds in DetectObjectBounds(rotated).Where(rect => !IsSimilarCrop(rect, contentBounds)).Take(3))
        {
            cropRects.Add(($"object-{objectIndex++}", objectBounds));
        }

        if (customCrop is not null)
        {
            cropRects.Add(("custom", customCrop.Value));
        }

        foreach (var (name, rect) in cropRects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var crop = Crop(rotated, Clamp(rect, rotated.Width, rotated.Height));
            variants.Add(SaveVariant(crop, request.SourcePath, runRoot, OcrImageKind.Cropped, $"{name}-crop", rect, rotation));

            using var grayscale = Sharpen(AdjustContrast(ToGrayscale(crop), 1.25f));
            variants.Add(SaveVariant(grayscale, request.SourcePath, runRoot, OcrImageKind.Deskewed, $"{name}-grayscale", rect, rotation));

            using var threshold = Sharpen(Threshold(AdjustContrast(BoxBlur(ToGrayscale(crop)), 1.35f), 142));
            variants.Add(SaveVariant(threshold, request.SourcePath, runRoot, OcrImageKind.Thresholded, $"{name}-threshold", rect, rotation));
        }

        logger.LogInformation("Prepared {Count} OCR image variants for {Path}", variants.Count, request.SourcePath);
        return Task.FromResult(new ImagePreprocessResult(variants, warnings));
    }

    private static ImagePreprocessVariant SaveVariant(Bitmap image, string sourcePath, string runRoot, OcrImageKind kind, string profile, Rectangle crop, double rotation)
    {
        var path = Path.Combine(runRoot, FilenameSanitizer.Sanitize(profile, 80) + ".png");
        image.Save(path, ImageFormat.Png);
        var cropJson = JsonSerializer.Serialize(new CropRectangle(crop.X, crop.Y, crop.Width, crop.Height));
        var transform = JsonSerializer.Serialize(new TransformMatrix(rotation, [1d, 0d, 0d, 1d, 0d, 0d]));
        return new ImagePreprocessVariant(kind, profile, sourcePath, path, cropJson, rotation, transform, image.Width, image.Height);
    }

    private static void ApplyExifOrientation(Image image)
    {
        const int orientationId = 0x0112;
        if (!image.PropertyIdList.Contains(orientationId))
        {
            return;
        }

        var prop = image.GetPropertyItem(orientationId);
        if (prop?.Value is null || prop.Value.Length < 2)
        {
            return;
        }

        var orientation = BitConverter.ToUInt16(prop.Value, 0);
        var flip = orientation switch
        {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.Rotate180FlipX,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => RotateFlipType.RotateNoneFlipNone
        };
        image.RotateFlip(flip);
        try { image.RemovePropertyItem(orientationId); } catch { }
    }

    private static Bitmap RotateForProfile(Bitmap source, OcrProfile profile, out double rotation)
    {
        var cardProfile = profile is OcrProfile.TradingCardFront or OcrProfile.TradingCardBack or OcrProfile.CardNumber or OcrProfile.SerialNumber or OcrProfile.GradingLabel;
        rotation = cardProfile && source.Width > source.Height * 1.15 ? 90 : 0;
        var copy = new Bitmap(source);
        if (Math.Abs(rotation - 90) < 0.001)
        {
            copy.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }

        return copy;
    }

    private static Rectangle DetectContentBounds(Bitmap image)
    {
        var border = EstimateBorderLuminance(image);
        const int tolerance = 24;
        var minX = image.Width - 1;
        var minY = image.Height - 1;
        var maxX = 0;
        var maxY = 0;

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                if (Math.Abs(Luminance(image.GetPixel(x, y)) - border) <= tolerance)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX <= minX || maxY <= minY)
        {
            return new Rectangle(0, 0, image.Width, image.Height);
        }

        var padX = Math.Max(4, (maxX - minX) / 80);
        var padY = Math.Max(4, (maxY - minY) / 80);
        return Clamp(new Rectangle(minX - padX, minY - padY, maxX - minX + padX * 2, maxY - minY + padY * 2), image.Width, image.Height);
    }

    private static IReadOnlyList<Rectangle> DetectObjectBounds(Bitmap image)
    {
        var background = EstimateBorderLuminance(image);
        var gridSize = Math.Clamp(Math.Min(image.Width, image.Height) / 160, 6, 18);
        var cols = (int)Math.Ceiling(image.Width / (double)gridSize);
        var rows = (int)Math.Ceiling(image.Height / (double)gridSize);
        var foreground = new bool[cols, rows];
        var visited = new bool[cols, rows];

        for (var gy = 0; gy < rows; gy++)
        {
            for (var gx = 0; gx < cols; gx++)
            {
                var x = Math.Min(image.Width - 1, gx * gridSize + gridSize / 2);
                var y = Math.Min(image.Height - 1, gy * gridSize + gridSize / 2);
                var luminance = Luminance(image.GetPixel(x, y));
                foreground[gx, gy] = Math.Abs(luminance - background) > 30;
            }
        }

        var components = new List<Rectangle>();
        for (var gy = 0; gy < rows; gy++)
        {
            for (var gx = 0; gx < cols; gx++)
            {
                if (!foreground[gx, gy] || visited[gx, gy])
                {
                    continue;
                }

                var minX = gx;
                var maxX = gx;
                var minY = gy;
                var maxY = gy;
                var count = 0;
                var stack = new Stack<(int X, int Y)>();
                stack.Push((gx, gy));
                visited[gx, gy] = true;

                while (stack.Count > 0)
                {
                    var (cx, cy) = stack.Pop();
                    count++;
                    minX = Math.Min(minX, cx);
                    maxX = Math.Max(maxX, cx);
                    minY = Math.Min(minY, cy);
                    maxY = Math.Max(maxY, cy);

                    TryPush(cx + 1, cy);
                    TryPush(cx - 1, cy);
                    TryPush(cx, cy + 1);
                    TryPush(cx, cy - 1);
                }

                var rect = Clamp(
                    new Rectangle(
                        minX * gridSize - gridSize,
                        minY * gridSize - gridSize,
                        (maxX - minX + 3) * gridSize,
                        (maxY - minY + 3) * gridSize),
                    image.Width,
                    image.Height);

                var minDimension = Math.Min(image.Width, image.Height);
                if (count >= 4 && rect.Width >= minDimension * 0.12 && rect.Height >= minDimension * 0.12)
                {
                    components.Add(rect);
                }

                void TryPush(int x, int y)
                {
                    if (x < 0 || x >= cols || y < 0 || y >= rows || visited[x, y] || !foreground[x, y])
                    {
                        return;
                    }

                    visited[x, y] = true;
                    stack.Push((x, y));
                }
            }
        }

        return components
            .OrderByDescending(rect => rect.Width * rect.Height)
            .ToList();
    }

    private static bool IsSimilarCrop(Rectangle left, Rectangle right)
    {
        var intersection = Rectangle.Intersect(left, right);
        if (intersection.IsEmpty)
        {
            return false;
        }

        var intersectionArea = intersection.Width * intersection.Height;
        var unionArea = left.Width * left.Height + right.Width * right.Height - intersectionArea;
        return unionArea > 0 && intersectionArea / (double)unionArea > 0.86;
    }

    private static double EstimateBorderLuminance(Bitmap image)
    {
        var samples = new List<double>();
        var stepX = Math.Max(1, image.Width / 20);
        var stepY = Math.Max(1, image.Height / 20);
        for (var x = 0; x < image.Width; x += stepX)
        {
            samples.Add(Luminance(image.GetPixel(x, 0)));
            samples.Add(Luminance(image.GetPixel(x, image.Height - 1)));
        }

        for (var y = 0; y < image.Height; y += stepY)
        {
            samples.Add(Luminance(image.GetPixel(0, y)));
            samples.Add(Luminance(image.GetPixel(image.Width - 1, y)));
        }

        return samples.Count == 0 ? 255 : samples.OrderBy(x => x).ElementAt(samples.Count / 2);
    }

    private static Bitmap Crop(Bitmap source, Rectangle rect) => source.Clone(rect, PixelFormat.Format32bppArgb);

    private static Bitmap ToGrayscale(Bitmap source)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                var gray = (int)Math.Clamp(Luminance(pixel), 0, 255);
                output.SetPixel(x, y, Color.FromArgb(pixel.A, gray, gray, gray));
            }
        }

        return output;
    }

    private static Bitmap AdjustContrast(Bitmap source, float factor)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                var r = Contrast(pixel.R, factor);
                var g = Contrast(pixel.G, factor);
                var b = Contrast(pixel.B, factor);
                output.SetPixel(x, y, Color.FromArgb(pixel.A, r, g, b));
            }
        }

        source.Dispose();
        return output;
    }

    private static Bitmap Threshold(Bitmap source, int threshold)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                var value = Luminance(pixel) >= threshold ? 255 : 0;
                output.SetPixel(x, y, Color.FromArgb(pixel.A, value, value, value));
            }
        }

        source.Dispose();
        return output;
    }

    private static Bitmap BoxBlur(Bitmap source)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var sum = 0;
                var count = 0;
                for (var yy = Math.Max(0, y - 1); yy <= Math.Min(source.Height - 1, y + 1); yy++)
                {
                    for (var xx = Math.Max(0, x - 1); xx <= Math.Min(source.Width - 1, x + 1); xx++)
                    {
                        sum += source.GetPixel(xx, yy).R;
                        count++;
                    }
                }

                var value = sum / count;
                output.SetPixel(x, y, Color.FromArgb(source.GetPixel(x, y).A, value, value, value));
            }
        }

        source.Dispose();
        return output;
    }

    private static Bitmap Sharpen(Bitmap source)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        var kernel = new[,] { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var value = 0;
                for (var ky = -1; ky <= 1; ky++)
                {
                    for (var kx = -1; kx <= 1; kx++)
                    {
                        var sx = Math.Clamp(x + kx, 0, source.Width - 1);
                        var sy = Math.Clamp(y + ky, 0, source.Height - 1);
                        value += source.GetPixel(sx, sy).R * kernel[ky + 1, kx + 1];
                    }
                }

                value = Math.Clamp(value, 0, 255);
                output.SetPixel(x, y, Color.FromArgb(source.GetPixel(x, y).A, value, value, value));
            }
        }

        source.Dispose();
        return output;
    }

    private static int Contrast(int value, float factor) => Math.Clamp((int)((value - 128) * factor + 128), 0, 255);
    private static double Luminance(Color pixel) => pixel.R * 0.2126 + pixel.G * 0.7152 + pixel.B * 0.0722;

    private static Rectangle? TryParseCrop(string json, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var crop = JsonSerializer.Deserialize<CropRectangle>(json);
            return crop is null || crop.Width <= 0 || crop.Height <= 0
                ? null
                : Clamp(new Rectangle(crop.X, crop.Y, crop.Width, crop.Height), width, height);
        }
        catch
        {
            return null;
        }
    }

    private static Rectangle Clamp(Rectangle rect, int width, int height)
    {
        var x = Math.Clamp(rect.X, 0, Math.Max(0, width - 1));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, height - 1));
        var right = Math.Clamp(rect.Right, x + 1, width);
        var bottom = Math.Clamp(rect.Bottom, y + 1, height);
        return new Rectangle(x, y, right - x, bottom - y);
    }

    private sealed record CropRectangle(int X, int Y, int Width, int Height);
    private sealed record TransformMatrix(double RotationDegrees, double[] Matrix);
}

