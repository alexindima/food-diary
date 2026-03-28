using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

public class RecipeRepository(FoodDiaryDbContext context) : IRecipeRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync(cancellationToken);
        return recipe;
    }

    public async Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default) {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Max(limit, 1);

        var query = context.Recipes
            .AsNoTracking()
            .Where(includePublic
                ? r => r.UserId == userId || r.Visibility == Visibility.Public
                : r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search)) {
            var normalized = $"%{EscapeLikePattern(search.Trim())}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, normalized, LikeEscapeCharacter) ||
                EF.Functions.ILike(r.Category ?? string.Empty, normalized, LikeEscapeCharacter) ||
                EF.Functions.ILike(r.Description ?? string.Empty, normalized, LikeEscapeCharacter));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var orderedQuery = query.OrderByDescending(r => r.CreatedOnUtc);
        var skip = (pageNumber - 1) * pageSize;

        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new {
                Recipe = r,
                UsageCount = r.MealItems.Count + r.NestedRecipeUsages.Count
            })
            .ToListAsync(cancellationToken);

        return (items.Select(i => (i.Recipe, i.UsageCount)).ToList(), totalItems);
    }

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
            cancellationToken);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        context.Recipes.Update(recipe);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        context.Recipes.Remove(recipe);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) {
        var entry = context.Entry(recipe);
        if (entry.State == EntityState.Detached) {
            context.Attach(recipe);
            entry = context.Entry(recipe);
        }

        entry.Property(r => r.TotalCalories).IsModified = true;
        entry.Property(r => r.TotalProteins).IsModified = true;
        entry.Property(r => r.TotalFats).IsModified = true;
        entry.Property(r => r.TotalCarbs).IsModified = true;
        entry.Property(r => r.TotalFiber).IsModified = true;
        entry.Property(r => r.TotalAlcohol).IsModified = true;

        await context.SaveChangesAsync(cancellationToken);
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

        var query = context.Recipes.AsNoTracking();
        query = query.Where(r => recipeIds.Contains(r.Id) && (includePublic
            ? r.UserId == userId || r.Visibility == Visibility.Public
            : r.UserId == userId));

        var recipes = await query.ToListAsync(cancellationToken);
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

        var items = await context.Recipes
            .AsNoTracking()
            .Where(r => recipeIds.Contains(r.Id) && (includePublic
                ? r.UserId == userId || r.Visibility == Visibility.Public
                : r.UserId == userId))
            .Select(r => new {
                Recipe = r,
                UsageCount = r.MealItems.Count + r.NestedRecipeUsages.Count
            })
            .ToListAsync(cancellationToken);

        return items.ToDictionary(x => x.Recipe.Id, x => (x.Recipe, x.UsageCount));
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
