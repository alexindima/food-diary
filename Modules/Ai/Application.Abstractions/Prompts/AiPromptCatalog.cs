using System.Text.RegularExpressions;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Prompts;

public static partial class AiPromptCatalog {
    public static IReadOnlyList<string> Keys { get; } = ["vision", "text-parse", "nutrition"];

    public static string? GetDefault(string key) => key.ToLowerInvariant() switch {
        "vision" => "Analyze the food photo and return only JSON with list of items. Each item must include nameEn, nameLocal, amount, unit, confidence (0-1). Use grams (g) when possible. {{languageHint}}",
        "text-parse" => "Parse the following food description into structured items: \"{{userText}}\". Return only JSON with list of items. Each item must include nameEn, nameLocal, amount, unit, confidence (0-1). Use grams (g) when possible. Estimate typical portion sizes for items without explicit amounts. {{languageHint}}",
        "nutrition" => "You are a nutrition assistant. Using the provided items with amounts, estimate calories and nutrients per item and totals. Item names are in English. Return only JSON.",
        _ => null,
    };

    public static IReadOnlyList<string> GetVariables(string key) => key switch {
        "vision" => ["languageHint", "descriptionHint"],
        "text-parse" => ["userText", "languageHint"],
        "nutrition" => ["itemsJson"],
        _ => [],
    };

    public static bool IsValid(string key, string text) => Keys.Contains(key, StringComparer.Ordinal)
        && !string.IsNullOrWhiteSpace(text) && text.Length <= 4096
        && VariablePattern().Matches(text).All(match => GetVariables(key).Contains(match.Groups["variable"].Value, StringComparer.Ordinal))
        && (!string.Equals(key, "text-parse", StringComparison.Ordinal) || text.Contains("{{userText}}", StringComparison.Ordinal));

    [GeneratedRegex(@"\{\{(?<variable>.*?)\}\}", RegexOptions.Singleline | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex VariablePattern();
}
