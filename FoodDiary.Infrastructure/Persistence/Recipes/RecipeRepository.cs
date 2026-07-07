using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

public sealed class RecipeRepository(FoodDiaryDbContext context) : IRecipeRepository {
    public async Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        await context.Recipes.AddAsync(recipe, cancellationToken).ConfigureAwait(false);
        return recipe;
    }

    private static IQueryable<Recipe> IncludeStepsAndIngredients(IQueryable<Recipe> query) =>
        query.AsSplitQuery()
            .Include(r => r.Steps)
            .ThenInclude(s => s.Ingredients)
            .ThenInclude(i => i.Product)
            .Include(r => r.Steps)
            .ThenInclude(s => s.Ingredients)
            .ThenInclude(i => i.NestedRecipe);

    public async Task<Recipe?> GetByIdAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = true,
        bool includeSteps = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<Recipe> query = context.Recipes;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        query = query.AsSplitQuery();

        if (includeSteps) {
            query = IncludeStepsAndIngredients(query);
        }

        return await query.FirstOrDefaultAsync(
            r => r.Id == id && (includePublic
                ? r.UserId == userId || r.Visibility == Visibility.Public
                : r.UserId == userId),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        context.Recipes.Update(recipe);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        Recipe? tracked = await context.Recipes.FindAsync([recipe.Id], cancellationToken).ConfigureAwait(false);
        if (tracked is not null) {
            context.Recipes.Remove(tracked);
        }
    }

    public async Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        EntityEntry<Recipe> entry = context.Entry(recipe);
        if (entry.State == EntityState.Detached) {
            Recipe? existing = await context.Recipes
                .FirstOrDefaultAsync(r => r.Id == recipe.Id, cancellationToken).ConfigureAwait(false);

            if (existing is null) {
                throw new DbUpdateConcurrencyException($"Recipe '{recipe.Id.Value}' was not found while updating nutrition.");
            }

            entry = context.Entry(existing);
            entry.CurrentValues.SetValues(recipe);
        }

        entry.Property(r => r.TotalCalories).IsModified = true;
        entry.Property(r => r.TotalProteins).IsModified = true;
        entry.Property(r => r.TotalFats).IsModified = true;
        entry.Property(r => r.TotalCarbs).IsModified = true;
        entry.Property(r => r.TotalFiber).IsModified = true;
        entry.Property(r => r.TotalAlcohol).IsModified = true;
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var recipeIds = ids.Distinct().ToList();
        if (recipeIds.Count == 0) {
            return new Dictionary<RecipeId, Recipe>();
        }

        IQueryable<Recipe> query = context.Recipes.AsNoTracking();
        query = query.Where(r => recipeIds.Contains(r.Id) && (includePublic
            ? r.UserId == userId || r.Visibility == Visibility.Public
            : r.UserId == userId));

        List<Recipe> recipes = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        return recipes.ToDictionary(r => r.Id);
    }

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
            .Select(r => r.MealItems.Count + r.NestedRecipeUsages.Count)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

}
