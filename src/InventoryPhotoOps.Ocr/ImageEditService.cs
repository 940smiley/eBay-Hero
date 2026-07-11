#pragma warning disable CA1416
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;
using InventoryPhotoOps.Core.Models;
using InventoryPhotoOps.Core.Services;

namespace InventoryPhotoOps.Ocr;

public sealed class ImageEditService : IImageEditService
{
    public Task<ImageEditResult> ApplyAsync(ImageEditRequest request, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.SourcePath))
        {
            throw new FileNotFoundException("Image edit source was not found.", request.SourcePath);
        }

        if (request.OverwriteOriginal)
        {
            throw new InvalidOperationException("Overwrite-original requires a dedicated explicit command path and is disabled by default.");
        }

        var root = string.IsNullOrWhiteSpace(request.WorkingRoot)
            ? Path.Combine(Path.GetTempPath(), "InventoryPhotoOps", "edits")
            : request.WorkingRoot;
        var sessionRoot = Path.Combine(root, request.PhotoId, DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
        Directory.CreateDirectory(sessionRoot);

        using var initial = new Bitmap(request.SourcePath);
        Bitmap current = new(initial);
        var operations = new List<ImageEditOperation>();
        var currentInput = request.SourcePath;
        var sequence = 0;

        try
        {
            foreach (var command in request.Commands)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var next = ApplyCommand(current, command);
                current.Dispose();
                current = next;
                var output = Path.Combine(sessionRoot, $"{sequence:000}-{command.Kind}.png");
                current.Save(output, ImageFormat.Png);
                operations.Add(new ImageEditOperation
                {
                    Kind = command.Kind,
                    Sequence = sequence,
                    ParametersJson = command.ParametersJson,
                    InputPath = currentInput,
                    OutputPath = output,
                    UndoPath = currentInput
                });
                currentInput = output;
                sequence++;
            }

            if (operations.Count == 0)
            {
                var output = Path.Combine(sessionRoot, "000-copy.png");
                current.Save(output, ImageFormat.Png);
                currentInput = output;
            }
        }
        finally
        {
            current.Dispose();
        }

        return Task.FromResult(new ImageEditResult(currentInput, operations, false));
    }

    private static Bitmap ApplyCommand(Bitmap source, ImageEditCommand command) =>
        command.Kind switch
        {
            ImageEditOperationKind.RotateLeft => RotateFlip(source, RotateFlipType.Rotate270FlipNone),
            ImageEditOperationKind.RotateRight => RotateFlip(source, RotateFlipType.Rotate90FlipNone),
            ImageEditOperationKind.RotateArbitrary or ImageEditOperationKind.Straighten => Rotate(source, ReadValue<double>(command.ParametersJson, "degrees")),
            ImageEditOperationKind.Crop => Crop(source, ReadCrop(command.ParametersJson, source.Width, source.Height)),
            ImageEditOperationKind.PerspectiveCorrection => Rotate(source, ReadValue<double>(command.ParametersJson, "degrees")),
            ImageEditOperationKind.Reset => new Bitmap(source),
            ImageEditOperationKind.SaveDerivedCopy => new Bitmap(source),
            ImageEditOperationKind.OverwriteOriginal => throw new InvalidOperationException("Overwrite original is disabled in the nondestructive edit service."),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command.Kind, "Unsupported image edit command.")
        };

    private static Bitmap RotateFlip(Bitmap source, RotateFlipType flip)
    {
        var copy = new Bitmap(source);
        copy.RotateFlip(flip);
        return copy;
    }

    private static Bitmap Crop(Bitmap source, Rectangle rect) => source.Clone(rect, PixelFormat.Format32bppArgb);

    private static Bitmap Rotate(Bitmap source, double degrees)
    {
        if (Math.Abs(degrees) < 0.001)
        {
            return new Bitmap(source);
        }

        var radians = degrees * Math.PI / 180d;
        var cos = Math.Abs(Math.Cos(radians));
        var sin = Math.Abs(Math.Sin(radians));
        var newWidth = (int)Math.Round(source.Width * cos + source.Height * sin);
        var newHeight = (int)Math.Round(source.Width * sin + source.Height * cos);
        var output = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
        output.SetResolution(source.HorizontalResolution, source.VerticalResolution);
        using var graphics = Graphics.FromImage(output);
        graphics.Clear(Color.White);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.TranslateTransform(newWidth / 2f, newHeight / 2f);
        graphics.RotateTransform((float)degrees);
        graphics.TranslateTransform(-source.Width / 2f, -source.Height / 2f);
        graphics.DrawImage(source, 0, 0);
        return output;
    }

    private static T ReadValue<T>(string json, string name)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.TryGetProperty(name, out var value)
            ? value.Deserialize<T>()!
            : default!;
    }

    private static Rectangle ReadCrop(string json, int width, int height)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        var root = document.RootElement;
        var x = root.TryGetProperty("x", out var xProp) ? xProp.GetInt32() : 0;
        var y = root.TryGetProperty("y", out var yProp) ? yProp.GetInt32() : 0;
        var w = root.TryGetProperty("width", out var wProp) ? wProp.GetInt32() : width;
        var h = root.TryGetProperty("height", out var hProp) ? hProp.GetInt32() : height;
        x = Math.Clamp(x, 0, Math.Max(0, width - 1));
        y = Math.Clamp(y, 0, Math.Max(0, height - 1));
        w = Math.Clamp(w, 1, width - x);
        h = Math.Clamp(h, 1, height - y);
        return new Rectangle(x, y, w, h);
    }
}
