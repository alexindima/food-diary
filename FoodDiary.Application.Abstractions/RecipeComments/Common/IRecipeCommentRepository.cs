using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecipeComments.Common;

public interface IRecipeCommentRepository {
    Task<RecipeComment> AddAsync(RecipeComment comment, CancellationToken cancellationToken = default);

    Task<RecipeComment?> GetByIdAsync(
        RecipeCommentId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(RecipeComment comment, CancellationToken cancellationToken = default);

    Task DeleteAsync(RecipeComment comment, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RecipeComment> Items, int Total)> GetPagedByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}
