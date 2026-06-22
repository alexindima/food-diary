using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

public class RecipeRepository(FoodDiaryDbContext context) : IRecipeRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return recipe;
    }

    public async Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        RecipeQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);

        IQueryable<Recipe> query = IncludeStepsAndIngredients(context.Recipes.AsNoTracking())
            .Where(includePublic
                ? r => r.UserId == userId || r.Visibility == Visibility.Public
                : r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filters.Search)) {
            string normalized = $"%{EscapeLikePattern(filters.Search.Trim())}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, normalized, LikeEscapeCharacter) ||
                EF.Functions.ILike(r.Category ?? string.Empty, normalized, LikeEscapeCharacter) ||
                EF.Functions.ILike(r.Description ?? string.Empty, normalized, LikeEscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(filters.Category)) {
            string category = $"%{EscapeLikePattern(filters.Category.Trim())}%";
            query = query.Where(r => EF.Functions.ILike(r.Category ?? string.Empty, category, LikeEscapeCharacter));
        }

        if (filters.MaxTotalTime.HasValue) {
            int maxTotalTime = filters.MaxTotalTime.Value;
            query = query.Where(r => (r.PrepTime ?? 0) + (r.CookTime ?? 0) <= maxTotalTime);
        }

        if (filters.CaloriesFrom.HasValue) {
            query = query.Where(r => (r.ManualCalories ?? r.TotalCalories ?? 0) >= filters.CaloriesFrom.Value);
        }

        if (filters.CaloriesTo.HasValue) {
            query = query.Where(r => (r.ManualCalories ?? r.TotalCalories ?? 0) <= filters.CaloriesTo.Value);
        }

        if (filters.HasImage.HasValue) {
            query = filters.HasImage.Value
                ? query.Where(r => r.ImageUrl != null || r.ImageAssetId != null)
                : query.Where(r => r.ImageUrl == null && r.ImageAssetId == null);
        }

        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        IOrderedQueryable<Recipe> orderedQuery = query.OrderByDescending(r => r.CreatedOnUtc);
        int skip = (pageNumber - 1) * pageSize;

        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new {
                Recipe = r,
                UsageCount = r.MealItems.Count + r.NestedRecipeUsages.Count,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items.ConvertAll(i => (i.Recipe, i.UsageCount)), totalItems);
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
            query = query
                .Include(r => r.Steps)
                .ThenInclude(s => s.Ingredients)
                .ThenInclude(i => i.Product)
                .Include(r => r.Steps)
                .ThenInclude(s => s.Ingredients)
                .ThenInclude(i => i.NestedRecipe);
        }

        query = query
            .Include(r => r.MealItems)
            .Include(r => r.NestedRecipeUsages);

        return await query.FirstOrDefaultAsync(
            r => r.Id == id && (includePublic
                ? r.UserId == userId || r.Visibility == Visibility.Public
                : r.UserId == userId),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        context.Recipes.Update(recipe);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        Recipe? tracked = await context.Recipes.FindAsync([recipe.Id], cancellationToken).ConfigureAwait(false);
        if (tracked is not null) {
            context.Recipes.Remove(tracked);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var recipeIds = ids.Distinct().ToList();
        if (recipeIds.Count == 0) {
            return new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)>();
        }

        var items = await IncludeStepsAndIngredients(context.Recipes.AsNoTracking())
            .Where(r => recipeIds.Contains(r.Id) && (includePublic
                ? r.UserId == userId || r.Visibility == Visibility.Public
                : r.UserId == userId))
            .Select(r => new {
                Recipe = r,
                UsageCount = r.MealItems.Count + r.NestedRecipeUsages.Count,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items.ToDictionary(x => x.Recipe.Id, x => (x.Recipe, x.UsageCount));
    }

    public async Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
        int page,
        int limit,
        string? search,
        string? category,
        int? maxPrepTime,
        string sortBy,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);

        IQueryable<Recipe> query = IncludeStepsAndIngredients(context.Recipes.AsNoTracking())
            .Where(r => r.Visibility == Visibility.Public);

        if (!string.IsNullOrWhiteSpace(search)) {
            string pattern = $"%{EscapeLikePattern(search.Trim())}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, pattern, LikeEscapeCharacter) ||
                (r.Category != null && EF.Functions.ILike(r.Category, pattern, LikeEscapeCharacter)) ||
                (r.Description != null && EF.Functions.ILike(r.Description, pattern, LikeEscapeCharacter)));
        }

        if (!string.IsNullOrWhiteSpace(category)) {
            query = query.Where(r => r.Category != null && EF.Functions.ILike(r.Category, category, LikeEscapeCharacter));
        }

        if (maxPrepTime.HasValue) {
            query = query.Where(r => r.PrepTime <= maxPrepTime.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        IOrderedQueryable<Recipe> orderedQuery = string.Equals(sortBy, "popular", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(r => r.MealItems.Count + r.NestedRecipeUsages.Count).ThenByDescending(r => r.CreatedOnUtc)
            : query.OrderByDescending(r => r.CreatedOnUtc);

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { Recipe = r, UsageCount = r.MealItems.Count + r.NestedRecipeUsages.Count })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items.ConvertAll(x => (x.Recipe, x.UsageCount)), totalItems);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
