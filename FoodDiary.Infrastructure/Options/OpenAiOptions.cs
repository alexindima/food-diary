namespace FoodDiary.Infrastructure.Options;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; init; } = string.Empty;
    public string VisionModel { get; init; } = "gpt-5-mini";
    public string VisionFallbackModel { get; init; } = "gpt-4o";
    public string TextModel { get; init; } = "gpt-5-mini";
}
