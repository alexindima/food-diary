using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IRecipeSourceReader {
    Task<Result<RecipeSource>> ReadAsync(string sourceUrl, CancellationToken cancellationToken);
}
