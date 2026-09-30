using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.MealPlanning;

public sealed class MealPlanCatalogRecipeReader(ICompositionReadContext context) : IMealPlanCatalogRecipeReader {
    public async Task<IReadOnlyList<CatalogRecipeReadModel>> SearchAsync(string? search, int limit, CancellationToken cancellationToken) {
        string term = search?.Trim() ?? string.Empty;
        return await context.Recipes.AsNoTracking()
            .Where(recipe => recipe.Visibility == Visibility.Public && (term == string.Empty || recipe.Name.Contains(term)))
            .OrderBy(recipe => recipe.Name).ThenBy(recipe => recipe.Id).Take(Math.Clamp(limit, 1, 100))
            .Select(recipe => new CatalogRecipeReadModel(recipe.Id.Value, recipe.Name, recipe.Servings))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlySet<Guid>> GetPublicIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) {
        if (ids.Count == 0) { return new HashSet<Guid>(); }
        RecipeId[] typedIds = [.. ids.Select(id => new RecipeId(id))];
        List<Guid> found = await context.Recipes.AsNoTracking()
            .Where(recipe => recipe.Visibility == Visibility.Public && Enumerable.Contains(typedIds, recipe.Id))
            .Select(recipe => recipe.Id.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
        return found.ToHashSet();
    }
}
