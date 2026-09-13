using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Recipes;

public sealed class RecipeUsageQuery(FoodDiaryDbContext context) : IRecipeUsageQuery {
    public async Task<int> GetUsageCountAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        await context.Recipes
            .AsNoTracking()
            .Where(r => r.Id == id && (includePublic
                ? r.UserId == userId || r.Visibility == Visibility.Public
                : r.UserId == userId))
            .Select(r => context.MealItems.AsNoTracking().Count(item => item.RecipeId == r.Id) + r.NestedRecipeUsages.Count)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

}
