using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

public sealed class RecipeRepository(FoodDiaryDbContext context, IProductSnapshotReadService productSnapshots) : IRecipeRepository {
    public async Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        await context.Recipes.AddAsync(recipe, cancellationToken).ConfigureAwait(false);
        return recipe;
    }

    private static IQueryable<Recipe> IncludeStepsAndIngredients(IQueryable<Recipe> query) =>
        query.AsSplitQuery()
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

        if (includeSteps) {
            query = IncludeStepsAndIngredients(query);
        }

        Recipe? recipe = await query.FirstOrDefaultAsync(
            r => r.Id == id && (includePublic
                ? r.UserId == userId || r.Visibility == Visibility.Public
                : r.UserId == userId),
            cancellationToken).ConfigureAwait(false);
        if (includeSteps && recipe is not null) { await LoadProductSnapshotsAsync(recipe, cancellationToken).ConfigureAwait(false); }
        return recipe;
    }

    public async Task<Recipe?> GetByIdForUpdateAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = false,
        bool includeSteps = false,
        CancellationToken cancellationToken = default) {
        if (!context.Database.IsRelational() || context.Database.CurrentTransaction is null) {
            return await GetByIdAsync(
                id,
                userId,
                includePublic,
                includeSteps,
                asTracking: true,
                cancellationToken).ConfigureAwait(false);
        }

        IQueryable<Recipe> query = context.Recipes.FromSqlInterpolated(
            $"SELECT *, xmin FROM \"Recipes\" WHERE \"Id\" = {id.Value} FOR UPDATE");
        if (includeSteps) {
            query = IncludeStepsAndIngredients(query);
        }

        Recipe? result = await query.FirstOrDefaultAsync(
            recipe => recipe.UserId == userId || (includePublic && recipe.Visibility == Visibility.Public),
            cancellationToken).ConfigureAwait(false);
        if (includeSteps && result is not null) { await LoadProductSnapshotsAsync(result, cancellationToken).ConfigureAwait(false); }
        return result;
    }

    private async Task LoadProductSnapshotsAsync(Recipe recipe, CancellationToken cancellationToken) {
        RecipeIngredient[] ingredients = [.. recipe.Steps.SelectMany(step => step.Ingredients)];
        ProductId[] ids = [.. ingredients.Where(ingredient => ingredient.ProductId.HasValue)
            .Select(ingredient => ingredient.ProductId!.Value).Distinct()];
        if (ids.Length == 0) { return; }
        IReadOnlyDictionary<ProductId, ProductSnapshotReadModel> snapshots = await productSnapshots.GetByIdsAsync(
            ids, cancellationToken).ConfigureAwait(false);
        var products = snapshots.ToDictionary(pair => pair.Key,
            pair => new RecipeIngredientProductSnapshot(pair.Value.Id, pair.Value.Name, pair.Value.BaseUnit,
                pair.Value.BaseAmount, pair.Value.CaloriesPerBase, pair.Value.ProteinsPerBase, pair.Value.FatsPerBase,
                pair.Value.CarbsPerBase, pair.Value.FiberPerBase, pair.Value.AlcoholPerBase, pair.Value.Visibility, pair.Value.Category));
        foreach (RecipeIngredient ingredient in ingredients) {
            ingredient.SetProductSnapshot(ingredient.ProductId is { } id ? products.GetValueOrDefault(id) : null);
        }
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
            Recipe existing = await context.Recipes
                .FirstOrDefaultAsync(r => r.Id == recipe.Id, cancellationToken).ConfigureAwait(false) ?? throw new DbUpdateConcurrencyException($"Recipe '{recipe.Id.Value}' was not found while updating nutrition.");
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
            .Select(r => context.MealItems.AsNoTracking().Count(item => item.RecipeId == r.Id) + r.NestedRecipeUsages.Count)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

}
