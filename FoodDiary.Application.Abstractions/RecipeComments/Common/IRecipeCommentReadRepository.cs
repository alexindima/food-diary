using FoodDiary.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecipeComments.Common;

public interface IRecipeCommentReadRepository {
    Task<(IReadOnlyList<RecipeComment> Items, int Total)> GetPagedByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RecipeCommentReadModel> Items, int Total)> GetPagedReadModelsByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}
