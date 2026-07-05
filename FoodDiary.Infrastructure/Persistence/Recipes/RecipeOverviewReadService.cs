using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

internal sealed class RecipeOverviewReadService(FoodDiaryDbContext context) : IRecipeOverviewReadService {
    private const string LikeEscapeCharacter = "\\";

    public async Task<(IReadOnlyList<RecipeOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        RecipeQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);

        IQueryable<Recipe> query = ApplyFilters(CreateBaseQuery(userId, includePublic), filters);

        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<RecipeOverviewReadRow> rows = await ProjectRows(query
                .OrderByDescending(r => r.CreatedOnUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (rows.ConvertAll(row => ToReadItem(row, userId)), totalItems);
    }

    public async Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetByIdsWithUsageAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var recipeIds = ids.Distinct().ToList();
        if (recipeIds.Count == 0) {
            return new Dictionary<RecipeId, RecipeOverviewReadItem>();
        }

        List<RecipeOverviewReadRow> rows = await ProjectRows(CreateBaseQuery(userId, includePublic)
                .Where(r => recipeIds.Contains(r.Id)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows.ToDictionary(row => row.Id, row => ToReadItem(row, userId));
    }

    public async Task<(IReadOnlyList<RecipeOverviewReadItem> Items, int TotalItems)> GetExplorePagedAsync(
        UserId currentUserId,
        int page,
        int limit,
        string? search,
        string? category,
        int? maxPrepTime,
        string sortBy,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);

        IQueryable<Recipe> query = ApplyExploreFilters(
            context.Recipes
                .AsNoTracking()
                .AsSplitQuery()
                .Where(r => r.Visibility == Visibility.Public),
            search,
            category,
            maxPrepTime);

        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<Recipe> orderedQuery = string.Equals(sortBy, "popular", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(r => r.MealItems.Count + r.NestedRecipeUsages.Count).ThenByDescending(r => r.CreatedOnUtc)
            : query.OrderByDescending(r => r.CreatedOnUtc);

        List<RecipeOverviewReadRow> rows = await ProjectRows(orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (rows.ConvertAll(row => ToReadItem(row, currentUserId)), totalItems);
    }

    private IQueryable<Recipe> CreateBaseQuery(UserId userId, bool includePublic) =>
        context.Recipes
            .AsNoTracking()
            .AsSplitQuery()
            .Where(includePublic
                ? r => r.UserId == userId || r.Visibility == Visibility.Public
                : r => r.UserId == userId);

    private static IQueryable<Recipe> ApplyFilters(IQueryable<Recipe> query, RecipeQueryFilters filters) {
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

        return query;
    }

    private static IQueryable<Recipe> ApplyExploreFilters(
        IQueryable<Recipe> query,
        string? search,
        string? category,
        int? maxPrepTime) {
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

        return query;
    }

    private static IQueryable<RecipeOverviewReadRow> ProjectRows(IQueryable<Recipe> query) =>
        query.Select(recipe => new RecipeOverviewReadRow(
            recipe.Id, recipe.UserId, recipe.Name, recipe.Description, recipe.Comment,
            recipe.Category, recipe.ImageUrl, recipe.ImageAssetId, recipe.PrepTime, recipe.CookTime,
            recipe.Servings, recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs,
            recipe.TotalFiber, recipe.TotalAlcohol, recipe.IsNutritionAutoCalculated,
            recipe.ManualCalories, recipe.ManualProteins, recipe.ManualFats, recipe.ManualCarbs,
            recipe.ManualFiber, recipe.ManualAlcohol, recipe.Visibility,
            recipe.MealItems.Count + recipe.NestedRecipeUsages.Count, recipe.CreatedOnUtc,
            recipe.Steps
                .OrderBy(step => step.StepNumber)
                .Select(step => new RecipeOverviewStepReadItem(
                    step.Id.Value,
                    step.StepNumber,
                    step.Title,
                    step.Instruction,
                    step.ImageUrl,
                    step.ImageAssetId.HasValue ? step.ImageAssetId.Value.Value : null,
                    step.Ingredients.Select(ingredient => new RecipeOverviewIngredientReadItem(
                        ingredient.Id.Value,
                        ingredient.Amount,
                        ingredient.ProductId.HasValue ? ingredient.ProductId.Value.Value : null,
                        ingredient.Product != null ? ingredient.Product.Name : null,
                        ingredient.Product != null ? ingredient.Product.BaseUnit.ToString() : null,
                        ingredient.Product != null ? ingredient.Product.BaseAmount : null,
                        ingredient.Product != null ? ingredient.Product.CaloriesPerBase : null,
                        ingredient.Product != null ? ingredient.Product.ProteinsPerBase : null,
                        ingredient.Product != null ? ingredient.Product.FatsPerBase : null,
                        ingredient.Product != null ? ingredient.Product.CarbsPerBase : null,
                        ingredient.Product != null ? ingredient.Product.FiberPerBase : null,
                        ingredient.Product != null ? ingredient.Product.AlcoholPerBase : null,
                        ingredient.NestedRecipeId.HasValue ? ingredient.NestedRecipeId.Value.Value : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.Name : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.Servings : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalCalories : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalProteins : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalFats : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalCarbs : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalFiber : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalAlcohol : null))
                        .ToList()))
                .ToList()));

    private static RecipeOverviewReadItem ToReadItem(RecipeOverviewReadRow row, UserId currentUserId) {
        NutritionSummary nutrition = GetEffectiveNutrition(row);
        var quality = FoodQualityScore.Calculate(
            nutrition.TotalCalories ?? 0,
            nutrition.TotalProteins ?? 0,
            nutrition.TotalFats ?? 0,
            nutrition.TotalCarbs ?? 0,
            nutrition.TotalFiber ?? 0,
            nutrition.TotalAlcohol ?? 0);
        bool isOwnedByCurrentUser = row.UserId == currentUserId;

        return new RecipeOverviewReadItem(
            row.Id,
            row.UserId,
            row.Name,
            row.Description,
            isOwnedByCurrentUser ? row.Comment : null,
            row.Category,
            row.ImageUrl,
            row.ImageAssetId,
            row.PrepTime,
            row.CookTime,
            row.Servings,
            nutrition.TotalCalories,
            nutrition.TotalProteins,
            nutrition.TotalFats,
            nutrition.TotalCarbs,
            nutrition.TotalFiber,
            nutrition.TotalAlcohol,
            row.IsNutritionAutoCalculated,
            row.ManualCalories,
            row.ManualProteins,
            row.ManualFats,
            row.ManualCarbs,
            row.ManualFiber,
            row.ManualAlcohol,
            row.Visibility,
            row.UsageCount,
            row.CreatedOnUtc,
            isOwnedByCurrentUser,
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            row.Steps);
    }

    private static NutritionSummary GetEffectiveNutrition(RecipeOverviewReadRow row) {
        if (!row.IsNutritionAutoCalculated) {
            return new NutritionSummary(
                row.ManualCalories ?? row.TotalCalories,
                row.ManualProteins ?? row.TotalProteins,
                row.ManualFats ?? row.TotalFats,
                row.ManualCarbs ?? row.TotalCarbs,
                row.ManualFiber ?? row.TotalFiber,
                row.ManualAlcohol ?? row.TotalAlcohol);
        }

        NutritionSummary calculated = CalculateAutoNutrition(row.Steps);
        return calculated.HasValues
            ? calculated
            : new NutritionSummary(
                row.TotalCalories,
                row.TotalProteins,
                row.TotalFats,
                row.TotalCarbs,
                row.TotalFiber,
                row.TotalAlcohol);
    }

    private static NutritionSummary CalculateAutoNutrition(IReadOnlyList<RecipeOverviewStepReadItem> steps) {
        double totalCalories = 0;
        double totalProteins = 0;
        double totalFats = 0;
        double totalCarbs = 0;
        double totalFiber = 0;
        double totalAlcohol = 0;
        bool hasComputedValues = false;

        foreach (RecipeOverviewIngredientReadItem ingredient in steps.SelectMany(step => step.Ingredients)) {
            if (ingredient.ProductBaseAmount is > 0) {
                double factor = ingredient.Amount / ingredient.ProductBaseAmount.Value;
                totalCalories += (ingredient.ProductCaloriesPerBase ?? 0) * factor;
                totalProteins += (ingredient.ProductProteinsPerBase ?? 0) * factor;
                totalFats += (ingredient.ProductFatsPerBase ?? 0) * factor;
                totalCarbs += (ingredient.ProductCarbsPerBase ?? 0) * factor;
                totalFiber += (ingredient.ProductFiberPerBase ?? 0) * factor;
                totalAlcohol += (ingredient.ProductAlcoholPerBase ?? 0) * factor;
                hasComputedValues = true;
            } else if (ingredient.NestedRecipeServings is > 0) {
                double factor = ingredient.Amount / ingredient.NestedRecipeServings.Value;
                totalCalories += (ingredient.NestedRecipeTotalCalories ?? 0) * factor;
                totalProteins += (ingredient.NestedRecipeTotalProteins ?? 0) * factor;
                totalFats += (ingredient.NestedRecipeTotalFats ?? 0) * factor;
                totalCarbs += (ingredient.NestedRecipeTotalCarbs ?? 0) * factor;
                totalFiber += (ingredient.NestedRecipeTotalFiber ?? 0) * factor;
                totalAlcohol += (ingredient.NestedRecipeTotalAlcohol ?? 0) * factor;
                hasComputedValues = true;
            }
        }

        if (!hasComputedValues) {
            return NutritionSummary.Empty;
        }

        return new NutritionSummary(
            Math.Round(totalCalories, 2, MidpointRounding.ToEven),
            Math.Round(totalProteins, 2, MidpointRounding.ToEven),
            Math.Round(totalFats, 2, MidpointRounding.ToEven),
            Math.Round(totalCarbs, 2, MidpointRounding.ToEven),
            Math.Round(totalFiber, 2, MidpointRounding.ToEven),
            Math.Round(totalAlcohol, 2, MidpointRounding.ToEven));
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private sealed record RecipeOverviewReadRow(
        RecipeId Id,
        UserId UserId,
        string Name,
        string? Description,
        string? Comment,
        string? Category,
        string? ImageUrl,
        ImageAssetId? ImageAssetId,
        int? PrepTime,
        int? CookTime,
        int Servings,
        double? TotalCalories,
        double? TotalProteins,
        double? TotalFats,
        double? TotalCarbs,
        double? TotalFiber,
        double? TotalAlcohol,
        bool IsNutritionAutoCalculated,
        double? ManualCalories,
        double? ManualProteins,
        double? ManualFats,
        double? ManualCarbs,
        double? ManualFiber,
        double? ManualAlcohol,
        Visibility Visibility,
        int UsageCount,
        DateTime CreatedOnUtc,
        IReadOnlyList<RecipeOverviewStepReadItem> Steps);

    private sealed record NutritionSummary(
        double? TotalCalories,
        double? TotalProteins,
        double? TotalFats,
        double? TotalCarbs,
        double? TotalFiber,
        double? TotalAlcohol) {
        public static NutritionSummary Empty { get; } = new(
            TotalCalories: null,
            TotalProteins: null,
            TotalFats: null,
            TotalCarbs: null,
            TotalFiber: null,
            TotalAlcohol: null);

        public bool HasValues =>
            TotalCalories.HasValue ||
            TotalProteins.HasValue ||
            TotalFats.HasValue ||
            TotalCarbs.HasValue ||
            TotalFiber.HasValue ||
            TotalAlcohol.HasValue;
    }
}
