using System.Text.Json;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;

internal sealed class AiPromptPreviewRenderer : IAiPromptPreviewRenderer {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string GetResponseFormatJson(string key) {
        object text = key switch {
            "vision" or "text-parse" => OpenAiRequestFactory.BuildFoodVisionTextFormat(),
            "product-label" => OpenAiProductLabelRequest.BuildTextFormat(),
            "nutrition" => OpenAiRequestFactory.BuildFoodNutritionTextFormat(),
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
        return JsonSerializer.Serialize(JsonSerializer.SerializeToElement(text).GetProperty("format"), JsonOptions);
    }

    public string Render(AiPromptDraft draft) {
        object request = draft.Key switch {
            "product-label" => OpenAiProductLabelRequest.Build("preview", "image-placeholder", new ProductImageAnalysis([]), draft.Locale, draft.Text, draft.PromptText, 1),
            "vision" => OpenAiRequestFactory.BuildVisionRequest("preview", "image-placeholder", draft.Locale, draft.Text, draft.PromptText, 1),
            "text-parse" => OpenAiRequestFactory.BuildTextParseRequest("preview", draft.Text!, draft.Locale, draft.PromptText, 1),
            _ => OpenAiRequestFactory.BuildNutritionRequest("preview", draft.Items!, draft.PromptText, 1),
        };
        // Use the production request factory; expose only resolved text, never image bytes or provider credentials.
        JsonElement input = JsonSerializer.SerializeToElement(request).GetProperty("input")[0].GetProperty("content");
        return string.Join("\n\n", input.EnumerateArray().Where(part => string.Equals(part.GetProperty("type").GetString(), "input_text", StringComparison.Ordinal))
            .Select(part => part.GetProperty("text").GetString()));
    }
}
