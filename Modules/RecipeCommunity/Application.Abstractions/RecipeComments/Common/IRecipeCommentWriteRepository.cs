using FoodDiary.Modules.RecipeCommunity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;

namespace FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;

public interface IRecipeCommentWriteRepository {
    Task<RecipeComment> AddAsync(RecipeComment comment, CancellationToken cancellationToken = default);

    Task<RecipeComment?> GetByIdAsync(
        RecipeCommentId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(RecipeComment comment, CancellationToken cancellationToken = default);

    Task DeleteAsync(RecipeComment comment, CancellationToken cancellationToken = default);
}
