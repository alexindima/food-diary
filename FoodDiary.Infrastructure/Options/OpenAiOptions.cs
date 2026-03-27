namespace FoodDiary.Infrastructure.Options;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; init; } = string.Empty;
    public string VisionModel { get; init; } = "gpt-5-mini";
    public string VisionFallbackModel { get; init; } = "gpt-4o";
    public string TextModel { get; init; } = "gpt-5-mini";

    public static bool HasVisionFallbackWhenVisionModelConfigured(OpenAiOptions options) {
        return string.IsNullOrWhiteSpace(options.VisionModel) || !string.IsNullOrWhiteSpace(options.VisionFallbackModel);
    }

    public static bool HasTextModelWhenApiKeyConfigured(OpenAiOptions options) {
        return string.IsNullOrWhiteSpace(options.ApiKey) || !string.IsNullOrWhiteSpace(options.TextModel);
    }

    public static bool HasVisionModelWhenApiKeyConfigured(OpenAiOptions options) {
        return string.IsNullOrWhiteSpace(options.ApiKey) || !string.IsNullOrWhiteSpace(options.VisionModel);
    }
}
