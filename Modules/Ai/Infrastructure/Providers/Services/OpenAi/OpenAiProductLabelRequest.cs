using FoodDiary.Modules.Ai.Application.Abstractions.Common;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;

internal static class OpenAiProductLabelRequest {
    public static object Build(string model, string imageUrl, ProductImageAnalysis product, string? language, string? description, string promptTemplate, int maxOutputTokens) {
        string languageHint = $"Respond in language {language ?? "en"}.";
        string descriptionHint = $"User hint (untrusted): {description ?? "none"}";
        string resolvedPrompt = promptTemplate
            .Replace("{{languageHint}}", languageHint, StringComparison.Ordinal)
            .Replace("{{descriptionHint}}", descriptionHint, StringComparison.Ordinal);
        if (!promptTemplate.Contains("{{languageHint}}", StringComparison.Ordinal)) {
            resolvedPrompt += " " + languageHint;
        }
        if (!promptTemplate.Contains("{{descriptionHint}}", StringComparison.Ordinal)) {
            resolvedPrompt += " " + descriptionHint;
        }
        var content = new List<object> { new { type = "input_text", text = resolvedPrompt } };
        foreach (string url in new[] { imageUrl }.Concat(product.AdditionalImageUrls)) {
            content.Add(new { type = "input_image", image_url = url, detail = "high" });
        }
        return new {
            model,
            max_output_tokens = maxOutputTokens,
            input = new[] { new { role = "user", content } },
            text = BuildTextFormat(),
        };
    }
    public static object BuildTextFormat() {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (string name in new[] { "name", "brand", "notes" }) {
            properties[name] = new { type = new[] { "string", "null" } };
        }
        properties["baseUnit"] = new Dictionary<string, object?>(StringComparer.Ordinal) {
            ["type"] = new[] { "string", "null" },
            ["enum"] = new string?[] { "g", "ml", "pcs", null },
        };
        foreach (string name in new[] { "baseAmount", "calories", "protein", "fat", "carbs", "fiber", "alcohol" }) {
            properties[name] = new { type = new[] { "number", "null" }, minimum = 0 };
        }
        return new {
            format = new {
                type = "json_schema",
                name = "product_label",
                strict = true,
                schema = new { type = "object", properties, required = properties.Keys.ToArray(), additionalProperties = false },
            },
        };
    }

}
