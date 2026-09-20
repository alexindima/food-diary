using System.Text.Json;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class AiPromptPreviewRendererTests {
    [Fact]
    public void ResponseFormat_UnknownScenario_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new AiPromptPreviewRenderer().GetResponseFormatJson("unknown"));

    [Theory]
    [InlineData("vision", "food_vision", "centerX")]
    [InlineData("text-parse", "food_vision", "nameLocal")]
    [InlineData("nutrition", "food_nutrition", "calories")]
    public void ResponseFormat_ExposesStrictProductionSchema(string key, string name, string field) {
        using var format = JsonDocument.Parse(new AiPromptPreviewRenderer().GetResponseFormatJson(key));
        Assert.Equal(name, format.RootElement.GetProperty("name").GetString());
        Assert.True(format.RootElement.GetProperty("strict").GetBoolean());
        JsonElement item = format.RootElement.GetProperty("schema").GetProperty("properties").GetProperty("items").GetProperty("items");
        Assert.False(item.GetProperty("additionalProperties").GetBoolean());
        Assert.Contains(item.GetProperty("required").EnumerateArray(), value => string.Equals(value.GetString(), field, StringComparison.Ordinal));
    }

    [Fact]
    public void TextPreview_ResolvesSampleLanguageAndProductionSuffix() {
        var renderer = new AiPromptPreviewRenderer();
        string result = renderer.Render(new AiPromptDraft("text-parse", "ru", "Parse {{userText}}. {{languageHint}}", "apple", ImageAssetId: null, Items: null));
        Assert.Multiple(() => Assert.Contains("Parse apple", result, StringComparison.Ordinal),
            () => Assert.Contains("language 'ru'", result, StringComparison.Ordinal),
            () => Assert.Contains("because no image was provided", result, StringComparison.Ordinal),
            () => Assert.DoesNotContain("{{", result, StringComparison.Ordinal));
    }

    [Fact]
    public void VisionPreview_IncludesAutomaticHintAndLocationRulesButNoImageContent() {
        string result = new AiPromptPreviewRenderer().Render(new AiPromptDraft("vision", "en", "Find food", "large plate", Guid.NewGuid(), Items: null));
        Assert.Multiple(() => Assert.Contains("User hint: large plate", result, StringComparison.Ordinal),
            () => Assert.Contains("centerX", result, StringComparison.Ordinal),
            () => Assert.DoesNotContain("image-placeholder", result, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Estimate nutrients")]
    [InlineData("Estimate nutrients: {{itemsJson}}")]
    public void NutritionPreview_ContainsFoodAmountsWithOrWithoutVariable(string template) {
        FoodVisionItemModel[] items = [new("apple", NameLocal: null, Amount: 100, "g", Confidence: 1)];
        string result = new AiPromptPreviewRenderer().Render(new AiPromptDraft("nutrition", "en", template, Text: null, ImageAssetId: null, items));
        Assert.Multiple(() => Assert.Contains("apple", result, StringComparison.Ordinal),
            () => Assert.Contains("100", result, StringComparison.Ordinal),
            () => Assert.DoesNotContain("{{", result, StringComparison.Ordinal));
    }
}
