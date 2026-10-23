using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Recipes;

public sealed class RecipeUserPurgeRetentionReadService(ICompositionReadContext context) : IRecipeUserPurgeRetentionReadService {
    public async Task<IReadOnlyCollection<RecipeId>> GetRetainedRecipeIdsAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        RecipeId[] roots = await context.Recipes.AsNoTracking()
            .Where(recipe => recipe.UserId == sourceUserId)
            .Where(recipe =>
                context.RecipeIngredients.AsNoTracking().Any(ingredient => ingredient.NestedRecipeId == recipe.Id && ingredient.RecipeStep.Recipe.UserId != sourceUserId) ||
                context.MealItems.AsNoTracking().Any(item => item.RecipeId == recipe.Id && item.Meal.UserId != sourceUserId) ||
                context.MealPlans.AsNoTracking().Where(plan => plan.UserId == null || plan.UserId != sourceUserId)
                    .SelectMany(plan => plan.Days).SelectMany(day => day.Meals).Any(meal => meal.RecipeId == recipe.Id) ||
                context.FavoriteRecipes.AsNoTracking().Any(favorite => favorite.RecipeId == recipe.Id && favorite.UserId != sourceUserId) ||
                context.RecipeLikes.AsNoTracking().Any(like => like.RecipeId == recipe.Id && like.UserId != sourceUserId) ||
                context.RecipeComments.AsNoTracking().Any(comment => comment.RecipeId == recipe.Id && comment.UserId != sourceUserId))
            .Select(recipe => recipe.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        HashSet<RecipeId> retained = [.. roots];
        RecipeId[] frontier = roots;
        while (frontier.Length > 0) {
            RecipeId[] children = await context.RecipeIngredients.AsNoTracking()
                .Where(ingredient => Enumerable.Contains(frontier, ingredient.RecipeStep.RecipeId) && ingredient.NestedRecipeId != null)
                .Join(context.Recipes.AsNoTracking().Where(recipe => recipe.UserId == sourceUserId),
                    ingredient => ingredient.NestedRecipeId!.Value, recipe => recipe.Id, (_, recipe) => recipe.Id)
                .Distinct().ToArrayAsync(cancellationToken).ConfigureAwait(false);
            frontier = [.. children.Where(retained.Add)];
        }
        return [.. retained.OrderBy(id => id.Value)];
    }
}
