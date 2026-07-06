using FoodDiary.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecipeComments.Common;

public interface IRecipeCommentReadModelRepository {
    Task<(IReadOnlyList<RecipeCommentReadModel> Items, int Total)> GetPagedReadModelsByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}