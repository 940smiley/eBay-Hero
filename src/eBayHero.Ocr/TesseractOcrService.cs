using System.Diagnostics;
using System.Globalization;
using eBayHero.Core.Models;
using eBayHero.Core.Services;
using Microsoft.Extensions.Logging;

namespace eBayHero.Ocr;

public sealed class TesseractOcrService(
    ILogger<TesseractOcrService> logger,
    IImagePreprocessingService imagePreprocessingService) : IOcrService
{
    public async Task<OcrRunResult> RunAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        var tesseractPath = ResolveTesseractPath(request.TesseractPath);
        if (string.IsNullOrWhiteSpace(tesseractPath))
        {
            throw new FileNotFoundException("tesseract.exe was not found. Configure the Tesseract path in Settings.");
        }

        if (!File.Exists(request.ImagePath))
        {
            throw new FileNotFoundException("OCR image was not found.", request.ImagePath);
        }

        var preprocessRoot = string.IsNullOrWhiteSpace(request.WorkingRoot)
            ? Path.Combine(Path.GetTempPath(), "eBayHero", "ocr")
            : request.WorkingRoot;
        var preprocessing = await imagePreprocessingService.PrepareAsync(
            new ImagePreprocessRequest(request.PhotoId, request.ImagePath, request.Profile, preprocessRoot, request.CustomCropJson),
            cancellationToken);

        IReadOnlyList<ImagePreprocessVariant> variants = preprocessing.Variants.Count == 0
            ? new List<ImagePreprocessVariant> { new(OcrImageKind.Original, "original", request.ImagePath, request.ImagePath, string.Empty, 0, string.Empty, 0, 0) }
            : preprocessing.Variants;

        var candidates = new List<OcrCandidateResult>();
        foreach (var pass in BuildPasses(request.Profile))
        {
            var passVariants = SelectVariants(variants, pass).Take(4).ToList();
            foreach (var psm in pass.PageSegmentationModes)
            {
                foreach (var variant in passVariants)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var candidate = await RunTesseractPassAsync(
                        tesseractPath,
                        variant,
                        request.Language,
                        request.UserWordsPath,
                        pass.CropName,
                        pass.PreprocessingProfile,
                        psm,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(candidate.Text))
                    {
                        candidates.Add(candidate);
                    }
                }
            }
        }

        var ordered = candidates
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.AverageConfidence)
            .ToList();

        var bestText = MergeCandidateText(ordered);
        var confidence = ordered.Count == 0 ? 0 : Math.Round(ordered.Take(3).Average(c => c.AverageConfidence), 2);
        var errorsAndWarnings = preprocessing.Warnings.Concat(ordered.Select(c => c.ErrorOutput).Where(e => !string.IsNullOrWhiteSpace(e)));
        return new OcrRunResult(bestText, confidence, ordered, string.Join(Environment.NewLine, errorsAndWarnings));
    }

    private async Task<OcrCandidateResult> RunTesseractPassAsync(
        string tesseractPath,
        ImagePreprocessVariant variant,
        string language,
        string userWordsPath,
        string crop,
        string preprocessing,
        int psm,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = tesseractPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add(variant.DerivedPath);
        process.StartInfo.ArgumentList.Add("stdout");
        process.StartInfo.ArgumentList.Add("-l");
        process.StartInfo.ArgumentList.Add(string.IsNullOrWhiteSpace(language) ? "eng" : language);
        process.StartInfo.ArgumentList.Add("--psm");
        process.StartInfo.ArgumentList.Add(psm.ToString(CultureInfo.InvariantCulture));
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("preserve_interword_spaces=1");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("user_defined_dpi=300");

        if (!string.IsNullOrWhiteSpace(userWordsPath) && File.Exists(userWordsPath))
        {
            process.StartInfo.ArgumentList.Add("--user-words");
            process.StartInfo.ArgumentList.Add(userWordsPath);
        }

        process.StartInfo.ArgumentList.Add("tsv");

        try
        {
            process.Start();
            await using var _ = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Cancellation cleanup is best-effort.
                }
            });

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            stopwatch.Stop();

            if (process.ExitCode != 0)
            {
                logger.LogWarning("Tesseract exited with code {ExitCode}: {Error}", process.ExitCode, stderr);
            }

            var parsed = ParseTsv(stdout);
            var score = OcrCandidateScorer.Score(parsed.Text, parsed.AverageConfidence, psm);
            return new OcrCandidateResult(
                parsed.Text,
                stdout,
                parsed.AverageConfidence,
                string.IsNullOrWhiteSpace(crop) ? variant.ProfileName : crop,
                string.IsNullOrWhiteSpace(preprocessing) ? variant.ProfileName : preprocessing,
                psm,
                score,
                (int)stopwatch.ElapsedMilliseconds,
                stderr,
                variant.DerivedPath,
                variant.CropRectangleJson,
                variant.RotationDegrees,
                variant.TransformMatrixJson,
                parsed.WordBoxesJson);
        }
        finally
        {
            process.Dispose();
        }
    }

    private static (string Text, double AverageConfidence, string WordBoxesJson) ParseTsv(string tsv)
    {
        var lines = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var order = new List<string>();
        var confidences = new List<double>();
        var boxes = new List<WordBox>();

        foreach (var line in tsv.Split(["\r\n", "\n"], StringSplitOptions.None).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split('\t');
            if (columns.Length < 12 || columns[0] != "5")
            {
                continue;
            }

            var word = columns[11].Trim();
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            if (double.TryParse(columns[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var confidence) && confidence >= 0)
            {
                confidences.Add(confidence);
            }

            if (int.TryParse(columns[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var left) &&
                int.TryParse(columns[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var top) &&
                int.TryParse(columns[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) &&
                int.TryParse(columns[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            {
                boxes.Add(new WordBox(word, left, top, width, height, confidence));
            }

            var key = $"{columns[1]}:{columns[2]}:{columns[3]}:{columns[4]}";
            if (!lines.TryGetValue(key, out var words))
            {
                words = [];
                lines[key] = words;
                order.Add(key);
            }

            words.Add(word);
        }

        var textLines = order
            .Select(key => string.Join(' ', lines[key]).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var text = string.Join(Environment.NewLine, textLines).Trim();
        var average = confidences.Count == 0 ? 0 : Math.Round(confidences.Average(), 2);
        return (text, average, System.Text.Json.JsonSerializer.Serialize(boxes));
    }

    private static string MergeCandidateText(IReadOnlyList<OcrCandidateResult> candidates)
    {
        if (candidates.Count == 0)
        {
            return string.Empty;
        }

        var bestScore = candidates[0].Score;
        var lines = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates.Take(10))
        {
            if (candidate != candidates[0] && candidate.Score < bestScore - 18)
            {
                continue;
            }

            foreach (var rawLine in candidate.Text.Split(["\r\n", "\n"], StringSplitOptions.None))
            {
                var line = rawLine.Trim();
                if (!IsUsableLine(line))
                {
                    continue;
                }

                if (seen.Add(line))
                {
                    lines.Add(line);
                }

                if (lines.Count >= 16)
                {
                    break;
                }
            }
        }

        return lines.Count == 0 ? candidates[0].Text : string.Join(Environment.NewLine, lines);
    }

    private static bool IsUsableLine(string line)
    {
        if (line.Length is < 2 or > 120)
        {
            return false;
        }

        var meaningful = line.Count(char.IsLetterOrDigit);
        if (meaningful < 2)
        {
            return false;
        }

        var punctuation = line.Count(char.IsPunctuation);
        return punctuation <= line.Length * 0.50;
    }

    private static IReadOnlyList<OcrPass> BuildPasses(OcrProfile profile) => profile switch
    {
        OcrProfile.TradingCardFront => [
            new("full", "grayscale", [11, 6]),
            new("bottom-name", "threshold", [7, 11, 13]),
            new("top-title", "threshold", [7, 11])
        ],
        OcrProfile.TradingCardBack => [
            new("full", "grayscale", [6, 11]),
            new("top", "threshold", [6, 11]),
            new("bottom-number", "threshold", [6, 11, 7])
        ],
        OcrProfile.BookCover => [
            new("full", "grayscale", [6, 11]),
            new("upper-title", "threshold", [6, 11])
        ],
        OcrProfile.CardNumber or OcrProfile.SerialNumber => [
            new("number-region", "threshold", [7, 11, 13]),
            new("full", "grayscale", [11])
        ],
        OcrProfile.GradingLabel => [
            new("label", "threshold", [6, 7, 11]),
            new("full", "grayscale", [11])
        ],
        OcrProfile.FullImage => [
            new("full", "grayscale", [6, 11])
        ],
        OcrProfile.CustomRegion => [
            new("custom", "threshold", [6, 7, 11, 13]),
            new("custom", "grayscale", [6, 7, 11])
        ],
        _ => [
            new("full", "grayscale", [11, 6]),
            new("bottom", "threshold", [7, 11]),
            new("top", "threshold", [7, 11])
        ]
    };

    private static string ResolveTesseractPath(string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            @"E:\Apps\tesseract-ocr\tesseract.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tesseract-OCR", "tesseract.exe"),
            @"C:\Program Files\Tesseract-OCR\tesseract.exe"
        };

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    private static IEnumerable<ImagePreprocessVariant> SelectVariants(IReadOnlyList<ImagePreprocessVariant> variants, OcrPass pass)
    {
        var preprocessing = pass.PreprocessingProfile;
        var crop = pass.CropName;
        var matched = variants.Where(v =>
            v.ProfileName.Contains(preprocessing, StringComparison.OrdinalIgnoreCase) &&
            (crop.Equals("full", StringComparison.OrdinalIgnoreCase) || v.ProfileName.Contains(crop.Split('-')[0], StringComparison.OrdinalIgnoreCase)));

        var list = matched.ToList();
        return list.Count > 0
            ? list
            : variants.Where(v => v.ProfileName.Contains(preprocessing, StringComparison.OrdinalIgnoreCase)).DefaultIfEmpty(variants[0]);
    }

    private sealed record OcrPass(string CropName, string PreprocessingProfile, int[] PageSegmentationModes);
    private sealed record WordBox(string Text, int Left, int Top, int Width, int Height, double Confidence);
}

