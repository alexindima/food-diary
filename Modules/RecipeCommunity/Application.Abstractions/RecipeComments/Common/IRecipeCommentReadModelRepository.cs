using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Models;

namespace FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;

public interface IRecipeCommentReadModelRepository {
    Task<(IReadOnlyList<RecipeCommentReadModel> Items, int Total)> GetPagedReadModelsByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}
