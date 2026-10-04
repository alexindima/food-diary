using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IRecipeVideoProcessor {
    Task<Result<RecipeAudio>> ExtractAudioAsync(Stream? video, string? sourceUrl, CancellationToken cancellationToken);
}
