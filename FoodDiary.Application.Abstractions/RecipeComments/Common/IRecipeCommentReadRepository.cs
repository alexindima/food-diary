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

    async Task<(IReadOnlyList<RecipeCommentReadModel> Items, int Total)> GetPagedReadModelsByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        (IReadOnlyList<RecipeComment> items, int total) = await GetPagedByRecipeAsync(recipeId, page, limit, cancellationToken).ConfigureAwait(false);
        return ([
            .. items.Select(static comment => new RecipeCommentReadModel(
                comment.Id.Value,
                comment.RecipeId.Value,
                comment.UserId.Value,
                comment.User?.Username,
                comment.User?.FirstName,
                comment.Text,
                comment.CreatedOnUtc,
                comment.ModifiedOnUtc)),
        ], total);
    }
}
