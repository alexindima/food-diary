namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Options;

public sealed class RecipeVideoOptions {
    public const string SectionName = "RecipeVideo";
    public string FfmpegPath { get; init; } = "ffmpeg";
    public static bool IsValid(RecipeVideoOptions options) => !string.IsNullOrWhiteSpace(options.FfmpegPath);
}
