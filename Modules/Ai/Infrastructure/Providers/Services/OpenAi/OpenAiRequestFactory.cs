using System.Text.Json;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Integrations.Services.OpenAi;

internal static class OpenAiRequestFactory {
    public static object BuildVisionRequest(
        string model,
        string imageUrl,
        string? userLanguage,
        string? description,
        string promptTemplate,
        int maxOutputTokens) {
        string language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        string languageHint = !string.Equals(language, "en", StringComparison.Ordinal)
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";
        string descriptionHint = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : $"User hint: {description.Trim()}. ";
        const string locationHint =
            "For every visible food or drink, estimate its visual center in the image. " +
            "Return centerX and centerY normalized from 0 at the left/top edge to 1 at the right/bottom edge, " +
            "plus locationConfidence from 0 to 1. Use null for all three fields only when the item cannot be localized.";
        bool templateContainsDescription = promptTemplate.Contains("{{descriptionHint}}", StringComparison.Ordinal);
        string resolvedPrompt = promptTemplate
            .Replace("{{languageHint}}", languageHint, StringComparison.Ordinal)
            .Replace("{{descriptionHint}}", descriptionHint, StringComparison.Ordinal);
        string requestPrompt = templateContainsDescription
            ? resolvedPrompt
            : descriptionHint + resolvedPrompt;

        return new {
            model,
            max_output_tokens = maxOutputTokens,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new { type = "input_text", text = requestPrompt + " " + locationHint },
                        new { type = "input_image", image_url = imageUrl, detail = "high" },
                    },
                },
            },
            text = BuildFoodVisionTextFormat(),
        };
    }

    public static object BuildTextParseRequest(
        string model,
        string text,
        string? userLanguage,
        string promptTemplate,
        int maxOutputTokens) {
        string language = string.IsNullOrWhiteSpace(userLanguage) ? "en" : userLanguage.Trim().ToLowerInvariant();
        string languageHint = !string.Equals(language, "en", StringComparison.Ordinal)
            ? $"Return nameEn in English and nameLocal in language '{language}'."
            : "Return nameEn in English and set nameLocal to null.";
        string resolvedPrompt = promptTemplate
            .Replace("{{userText}}", text, StringComparison.Ordinal)
            .Replace("{{languageHint}}", languageHint, StringComparison.Ordinal);

        return new {
            model,
            max_output_tokens = maxOutputTokens,
            input = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "input_text",
                            text = resolvedPrompt + " Set centerX, centerY, and locationConfidence to null because no image was provided.",
                        },
                    },
                },
            },
            text = BuildFoodVisionTextFormat(),
        };
    }

    public static object BuildNutritionRequest(
        string model,
        IReadOnlyList<FoodVisionItemModel> items,
        string promptTemplate,
        int maxOutputTokens) {
        var mappedItems = items.Select(item => new {
            name = string.IsNullOrWhiteSpace(item.NameEn) ? item.NameLocal ?? "unknown" : item.NameEn,
            amount = item.Amount,
            unit = item.Unit,
        });
        string itemsJson = JsonSerializer.Serialize(new { items = mappedItems });
        bool templateContainsItems = promptTemplate.Contains("{{itemsJson}}", StringComparison.Ordinal);
        string resolvedPrompt = promptTemplate.Replace("{{itemsJson}}", itemsJson, StringComparison.Ordinal);
        string[] promptParts = templateContainsItems
            ? [resolvedPrompt]
            : [resolvedPrompt, itemsJson];

        return new {
            model,
            max_output_tokens = maxOutputTokens,
            input = new[] {
                new {
                    role = "user",
                    content = promptParts.Select(text => new { type = "input_text", text }).ToArray(),
                },
            },
            text = BuildFoodNutritionTextFormat(),
        };
    }

    public static object BuildFoodVisionTextFormat() =>
        new {
            format = new {
                type = "json_schema",
                name = "food_vision",
                schema = new {
                    type = "object",
                    properties = new {
                        items = new {
                            type = "array",
                            items = new {
                                type = "object",
                                properties = new {
                                    nameEn = new { type = "string" },
                                    nameLocal = new { type = new[] { "string", "null" } },
                                    amount = new { type = "number" },
                                    unit = new { type = "string" },
                                    confidence = new { type = "number" },
                                    centerX = new { type = new[] { "number", "null" }, minimum = 0, maximum = 1 },
                                    centerY = new { type = new[] { "number", "null" }, minimum = 0, maximum = 1 },
                                    locationConfidence = new { type = new[] { "number", "null" }, minimum = 0, maximum = 1 },
                                },
                                required = new[] {
                                    "nameEn", "nameLocal", "amount", "unit", "confidence",
                                    "centerX", "centerY", "locationConfidence",
                                },
                                additionalProperties = false,
                            },
                        },
                    },
                    required = new[] { "items" },
                    additionalProperties = false,
                },
                strict = true,
            },
        };

    public static object BuildFoodNutritionTextFormat() =>
        new {
            format = new {
                type = "json_schema",
                name = "food_nutrition",
                schema = new {
                    type = "object",
                    properties = new {
                        calories = new { type = "number" },
                        protein = new { type = "number" },
                        fat = new { type = "number" },
                        carbs = new { type = "number" },
                        fiber = new { type = "number" },
                        alcohol = new { type = "number" },
                        items = new {
                            type = "array",
                            items = new {
                                type = "object",
                                properties = new {
                                    name = new { type = "string" },
                                    amount = new { type = "number" },
                                    unit = new { type = "string" },
                                    calories = new { type = "number" },
                                    protein = new { type = "number" },
                                    fat = new { type = "number" },
                                    carbs = new { type = "number" },
                                    fiber = new { type = "number" },
                                    alcohol = new { type = "number" },
                                },
                                required = new[] {
                                    "name", "amount", "unit",
                                    "calories", "protein", "fat", "carbs", "fiber", "alcohol",
                                },
                                additionalProperties = false,
                            },
                        },
                    },
                    required = new[] { "calories", "protein", "fat", "carbs", "fiber", "alcohol", "items" },
                    additionalProperties = false,
                },
                strict = true,
            },
        };
}
