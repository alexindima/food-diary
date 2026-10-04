using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

public static partial class RecipeSourceTextExtractor {
    public static string Extract(string html) {
        // Structured recipes carry quantities and steps more reliably than surrounding page text.
        foreach (Match script in ScriptRegex().Matches(html)) {
            try {
                using var document = JsonDocument.Parse(script.Groups["body"].Value,
                    new JsonDocumentOptions { MaxDepth = 32 });
                JsonElement? recipe = FindRecipe(document.RootElement);
                if (recipe is not null) {
                    return recipe.Value.GetRawText();
                }
            } catch (JsonException) {
                // Other scripts are not recipe JSON-LD.
            }
        }
        // Description metadata belongs to the current page; embedded captions may belong to recommended posts.
        string? openGraphDescription = null;
        string? description = null;
        foreach (Match meta in MetaRegex().Matches(html)) {
            var attributes = AttributeRegex().Matches(meta.Value)
                .Cast<Match>().GroupBy(x => x.Groups["name"].Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => WebUtility.HtmlDecode(x.Last().Groups["value"].Value), StringComparer.OrdinalIgnoreCase);
            string kind = attributes.GetValueOrDefault("property") ?? attributes.GetValueOrDefault("name") ?? string.Empty;
            if (attributes.TryGetValue("content", out string? content) && !string.IsNullOrWhiteSpace(content)) {
                if (string.Equals(kind, "og:description", StringComparison.Ordinal)) {
                    openGraphDescription ??= content;
                } else if (string.Equals(kind, "description", StringComparison.Ordinal)) {
                    description ??= content;
                }
            }
        }
        if ((openGraphDescription ?? description) is { } pageDescription) {
            return pageDescription;
        }
        var captions = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match caption in CaptionRegex().Matches(html)) {
            try {
                string? text = JsonSerializer.Deserialize<string>(caption.Groups["text"].Value);
                if (!string.IsNullOrWhiteSpace(text)) {
                    captions.Add(text);
                    if (captions.Count > 1) {
                        // Without page metadata, choosing between unrelated posts could import the wrong recipe.
                        return string.Empty;
                    }
                }
            } catch (JsonException) {
                // Continue with other captions or visible page text.
            }
        }
        if (captions.Count == 1) {
            return captions.Single();
        }
        string withoutScripts = ScriptRegex().Replace(html, string.Empty);
        withoutScripts = StyleRegex().Replace(withoutScripts, string.Empty);
        string textOnly = TagRegex().Replace(withoutScripts, "\n");
        return string.Join('\n', WebUtility.HtmlDecode(textOnly).Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.Ordinal));
    }

    private static JsonElement? FindRecipe(JsonElement node) {
        if (node.ValueKind == JsonValueKind.Object) {
            if (node.TryGetProperty("@type", out JsonElement type) &&
                ((type.ValueKind == JsonValueKind.String && string.Equals(type.GetString(), "Recipe", StringComparison.Ordinal)) ||
                 (type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && string.Equals(x.GetString(), "Recipe", StringComparison.Ordinal))))) {
                return node;
            }
            foreach (JsonProperty property in node.EnumerateObject()) {
                JsonElement? found = FindRecipe(property.Value);
                if (found is not null) {
                    return found;
                }
            }
        } else if (node.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement child in node.EnumerateArray()) {
                JsonElement? found = FindRecipe(child);
                if (found is not null) {
                    return found;
                }
            }
        }
        return null;
    }

    [GeneratedRegex("<script\\b[^>]*>(?<body>.*?)</script\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking)]
    private static partial Regex ScriptRegex();
    [GeneratedRegex("<style\\b[^>]*>.*?</style\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking)]
    private static partial Regex StyleRegex();
    [GeneratedRegex("<meta\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex MetaRegex();
    [GeneratedRegex("""(?<name>[\w:-]+)\s*=\s*(?:"(?<value>[^"]*)"|'(?<value>[^']*)')""", RegexOptions.NonBacktracking)]
    private static partial Regex AttributeRegex();
    [GeneratedRegex("\"caption\"\\s*:\\s*\\{\\s*\"text\"\\s*:\\s*(?<text>\"(?:[^\"\\\\]|\\\\.)*\")", RegexOptions.NonBacktracking)]
    private static partial Regex CaptionRegex();
    [GeneratedRegex("<[^>]*>", RegexOptions.NonBacktracking)]
    private static partial Regex TagRegex();
}
