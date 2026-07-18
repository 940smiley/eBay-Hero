using System.Text.Json;
using System.Text.RegularExpressions;
using eBayHero.Core.Configuration;
using eBayHero.Core.Services;

namespace eBayHero.Ocr;

public sealed class OcrLearningService(InventoryOptions options) : IOcrLearningService
{
    public async Task SaveCorrectionAsync(string incorrectText, string correctedText, string field, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(correctedText))
        {
            return;
        }

        Directory.CreateDirectory(options.OcrTrainingRoot);
        var data = await LoadAsync(cancellationToken);
        var existing = data.Corrections.FirstOrDefault(c =>
            string.Equals(c.IncorrectText, incorrectText, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.CorrectedText, correctedText, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Field, field, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            data.Corrections.Add(new OcrLearningCorrection
            {
                IncorrectText = incorrectText.Trim(),
                CorrectedText = correctedText.Trim(),
                Field = field.Trim(),
                UsageCount = 1,
                CreatedUtc = DateTimeOffset.UtcNow,
                LastUsedUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.UsageCount++;
            existing.LastUsedUtc = DateTimeOffset.UtcNow;
        }

        foreach (var token in ExtractVocabulary(correctedText))
        {
            if (!data.Vocabulary.Any(v => string.Equals(v, token, StringComparison.OrdinalIgnoreCase)))
            {
                data.Vocabulary.Add(token);
            }
        }

        await SaveAsync(data, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> RegenerateUserWordsAsync(IEnumerable<string> inventoryVocabulary, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.OcrTrainingRoot);
        var data = await LoadAsync(cancellationToken);
        var words = data.Vocabulary
            .Concat(inventoryVocabulary.SelectMany(ExtractVocabulary))
            .Where(x => x.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await File.WriteAllLinesAsync(options.OcrUserWordsPath, words, cancellationToken);
        return words;
    }

    public string ApplyCorrections(string text, string field)
    {
        if (string.IsNullOrWhiteSpace(text) || !File.Exists(options.OcrLearningPath))
        {
            return text;
        }

        var json = File.ReadAllText(options.OcrLearningPath);
        var data = JsonSerializer.Deserialize<OcrLearningFile>(json) ?? new OcrLearningFile();
        var result = text;

        foreach (var correction in data.Corrections.OrderByDescending(c => c.IncorrectText.Length))
        {
            if (string.IsNullOrWhiteSpace(correction.IncorrectText) ||
                string.IsNullOrWhiteSpace(correction.CorrectedText))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(correction.Field) &&
                !string.Equals(correction.Field, field, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var pattern = $@"\b{Regex.Escape(correction.IncorrectText)}\b";
            result = Regex.Replace(result, pattern, correction.CorrectedText, RegexOptions.IgnoreCase);
        }

        return result;
    }

    private async Task<OcrLearningFile> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(options.OcrLearningPath))
        {
            return new OcrLearningFile();
        }

        var json = await File.ReadAllTextAsync(options.OcrLearningPath, cancellationToken);
        return JsonSerializer.Deserialize<OcrLearningFile>(json) ?? new OcrLearningFile();
    }

    private async Task SaveAsync(OcrLearningFile data, CancellationToken cancellationToken)
    {
        data.UpdatedUtc = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(options.OcrLearningPath, json, cancellationToken);
    }

    private static IEnumerable<string> ExtractVocabulary(string text)
    {
        foreach (Match match in Regex.Matches(text ?? string.Empty, @"[A-Za-z0-9][A-Za-z0-9'.-]{1,39}"))
        {
            yield return match.Value;
        }
    }

    private sealed class OcrLearningFile
    {
        public int SchemaVersion { get; set; } = 1;
        public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
        public List<OcrLearningCorrection> Corrections { get; set; } = [];
        public List<string> Vocabulary { get; set; } = [];
    }

    private sealed class OcrLearningCorrection
    {
        public string IncorrectText { get; set; } = string.Empty;
        public string CorrectedText { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset LastUsedUtc { get; set; }
    }
}


