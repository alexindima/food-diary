using FoodDiary.Modules.Recipes.Domain.Nutrition;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.FoodQuality.ValueObjects;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

internal sealed class RecipeOverviewReadService(ICompositionReadContext context) : IRecipeOverviewReadService {
    private const string LikeEscapeCharacter = "\\";

    public async Task<(IReadOnlyList<RecipeOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        RecipeQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);

        IQueryable<Recipe> query = ApplyFilters(CreateBaseQuery(userId, includePublic), filters);

        int totalItems = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        List<RecipeOverviewReadRow> rows = await ProjectRows(query.AsNoTracking()
                .OrderByDescending(r => r.CreatedOnUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize), userId)
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

        List<RecipeOverviewReadRow> rows = await ProjectRows(CreateBaseQuery(userId, includePublic).AsNoTracking()
                .Where(r => recipeIds.Contains(r.Id)), userId)
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
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1, maxPageSize: 50);

        IQueryable<Recipe> query = ApplyExploreFilters(
            context.Recipes
                .AsNoTracking()
                .AsSplitQuery()
                .Where(r => r.Visibility == Visibility.Public),
            search,
            category,
            maxPrepTime);

        int totalItems = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<Recipe> orderedQuery = string.Equals(sortBy, "popular", StringComparison.OrdinalIgnoreCase)
            ? query.AsNoTracking().OrderByDescending(r => context.MealItems.AsNoTracking().Count(item => item.RecipeId == r.Id) + r.NestedRecipeUsages.Count).ThenByDescending(r => r.CreatedOnUtc)
            : query.AsNoTracking().OrderByDescending(r => r.CreatedOnUtc);

        List<RecipeOverviewReadRow> rows = await ProjectRows(orderedQuery.AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize), currentUserId)
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
            query = query.AsNoTracking().Where(r =>
                EF.Functions.ILike(r.Name, normalized, LikeEscapeCharacter) ||
                EF.Functions.ILike(r.Category ?? string.Empty, normalized, LikeEscapeCharacter) ||
                EF.Functions.ILike(r.Description ?? string.Empty, normalized, LikeEscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(filters.Category)) {
            string category = $"%{EscapeLikePattern(filters.Category.Trim())}%";
            query = query.AsNoTracking().Where(r => EF.Functions.ILike(r.Category ?? string.Empty, category, LikeEscapeCharacter));
        }

        if (filters.MaxTotalTime.HasValue) {
            int maxTotalTime = filters.MaxTotalTime.Value;
            query = query.AsNoTracking().Where(r => (r.PrepTime ?? 0) + (r.CookTime ?? 0) <= maxTotalTime);
        }

        if (filters.CaloriesFrom.HasValue) {
            query = query.AsNoTracking().Where(r => (r.ManualCalories ?? r.TotalCalories ?? 0) >= filters.CaloriesFrom.Value);
        }

        if (filters.CaloriesTo.HasValue) {
            query = query.AsNoTracking().Where(r => (r.ManualCalories ?? r.TotalCalories ?? 0) <= filters.CaloriesTo.Value);
        }

        if (filters.HasImage.HasValue) {
            query = filters.HasImage.Value
                ? query.AsNoTracking().Where(r => r.ImageUrl != null || r.ImageAssetId != null)
                : query.AsNoTracking().Where(r => r.ImageUrl == null && r.ImageAssetId == null);
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
            query = query.AsNoTracking().Where(r =>
                EF.Functions.ILike(r.Name, pattern, LikeEscapeCharacter) ||
                (r.Category != null && EF.Functions.ILike(r.Category, pattern, LikeEscapeCharacter)) ||
                (r.Description != null && EF.Functions.ILike(r.Description, pattern, LikeEscapeCharacter)));
        }

        if (!string.IsNullOrWhiteSpace(category)) {
            query = query.AsNoTracking().Where(r => r.Category != null && EF.Functions.ILike(r.Category, category, LikeEscapeCharacter));
        }

        if (maxPrepTime.HasValue) {
            query = query.AsNoTracking().Where(r => r.PrepTime <= maxPrepTime.Value);
        }

        return query;
    }

    private IQueryable<RecipeOverviewReadRow> ProjectRows(IQueryable<Recipe> query, UserId currentUserId) =>
        query.AsNoTracking().Select(recipe => new RecipeOverviewReadRow(
            recipe.Id, recipe.UserId, recipe.Name, recipe.Description, recipe.Comment,
            recipe.Category, recipe.ImageUrl, recipe.ImageAssetId, recipe.PrepTime, recipe.CookTime,
            recipe.Servings, recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs,
            recipe.TotalFiber, recipe.TotalAlcohol, recipe.IsNutritionAutoCalculated,
            recipe.ManualCalories, recipe.ManualProteins, recipe.ManualFats, recipe.ManualCarbs,
            recipe.ManualFiber, recipe.ManualAlcohol, recipe.Visibility,
            context.MealItems.AsNoTracking().Count(item => item.RecipeId == recipe.Id) + recipe.NestedRecipeUsages.Count, recipe.CreatedOnUtc,
            recipe.Steps
                .OrderBy(step => step.StepNumber)
                .Select(step => new RecipeOverviewStepReadItem(
                    step.Id.Value,
                    step.StepNumber,
                    step.Title,
                    step.Instruction,
                    step.ImageUrl,
                    step.ImageAssetId.HasValue ? step.ImageAssetId.Value.Value : null,
                    step.Ingredients.SelectMany(
                        ingredient => context.Products.AsNoTracking().Where(product => product.Id == ingredient.ProductId).DefaultIfEmpty(),
                        (ingredient, product) => new RecipeOverviewIngredientReadItem(
                        ingredient.Id.Value,
                        ingredient.Amount,
                        ingredient.ProductId.HasValue ? ingredient.ProductId.Value.Value : null,
                        product != null ? product.Name : null,
                        product != null ? product.BaseUnit.ToString() : null,
                        product != null ? product.BaseAmount : null,
                        product != null ? product.CaloriesPerBase : null,
                        product != null ? product.ProteinsPerBase : null,
                        product != null ? product.FatsPerBase : null,
                        product != null ? product.CarbsPerBase : null,
                        product != null ? product.FiberPerBase : null,
                        product != null ? product.AlcoholPerBase : null,
                        ingredient.NestedRecipeId.HasValue ? ingredient.NestedRecipeId.Value.Value : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.Name : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.Servings : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalCalories : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalProteins : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalFats : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalCarbs : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalFiber : null,
                        ingredient.NestedRecipe != null ? ingredient.NestedRecipe.TotalAlcohol : null,
                        product == null || product.UserId == currentUserId || product.Visibility == Visibility.Public,
                        ingredient.NestedRecipe == null || ingredient.NestedRecipe.UserId == currentUserId || ingredient.NestedRecipe.Visibility == Visibility.Public))
                        .ToList()))
                .ToList()));

    private static RecipeOverviewReadItem ToReadItem(RecipeOverviewReadRow row, UserId currentUserId) {
        RecipeNutritionValues nutrition = GetEffectiveNutrition(row);
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
            SanitizeSteps(row.Steps));
    }

    private static IReadOnlyList<RecipeOverviewStepReadItem> SanitizeSteps(
        IReadOnlyList<RecipeOverviewStepReadItem> steps) =>
        [.. steps.Select(step => step with {
            Ingredients = [.. step.Ingredients.Select(SanitizeIngredient)],
        })];

    private static RecipeOverviewIngredientReadItem SanitizeIngredient(RecipeOverviewIngredientReadItem ingredient) =>
        ingredient with {
            ProductId = ingredient.ProductIsAccessible ? ingredient.ProductId : null,
            ProductName = ingredient.ProductIsAccessible ? ingredient.ProductName : null,
            ProductBaseUnit = ingredient.ProductIsAccessible ? ingredient.ProductBaseUnit : null,
            ProductBaseAmount = ingredient.ProductIsAccessible ? ingredient.ProductBaseAmount : null,
            ProductCaloriesPerBase = ingredient.ProductIsAccessible ? ingredient.ProductCaloriesPerBase : null,
            ProductProteinsPerBase = ingredient.ProductIsAccessible ? ingredient.ProductProteinsPerBase : null,
            ProductFatsPerBase = ingredient.ProductIsAccessible ? ingredient.ProductFatsPerBase : null,
            ProductCarbsPerBase = ingredient.ProductIsAccessible ? ingredient.ProductCarbsPerBase : null,
            ProductFiberPerBase = ingredient.ProductIsAccessible ? ingredient.ProductFiberPerBase : null,
            ProductAlcoholPerBase = ingredient.ProductIsAccessible ? ingredient.ProductAlcoholPerBase : null,
            NestedRecipeId = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeId : null,
            NestedRecipeName = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeName : null,
            NestedRecipeServings = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeServings : null,
            NestedRecipeTotalCalories = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeTotalCalories : null,
            NestedRecipeTotalProteins = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeTotalProteins : null,
            NestedRecipeTotalFats = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeTotalFats : null,
            NestedRecipeTotalCarbs = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeTotalCarbs : null,
            NestedRecipeTotalFiber = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeTotalFiber : null,
            NestedRecipeTotalAlcohol = ingredient.NestedRecipeIsAccessible ? ingredient.NestedRecipeTotalAlcohol : null,
        };

    private static RecipeNutritionValues GetEffectiveNutrition(RecipeOverviewReadRow row) {
        var stored = new RecipeNutritionValues(row.TotalCalories, row.TotalProteins, row.TotalFats,
            row.TotalCarbs, row.TotalFiber, row.TotalAlcohol);
        if (!row.IsNutritionAutoCalculated) {
            return RecipeNutritionPolicy.SelectManual(
                new RecipeNutritionValues(row.ManualCalories, row.ManualProteins, row.ManualFats,
                    row.ManualCarbs, row.ManualFiber, row.ManualAlcohol), stored);
        }
        return RecipeNutritionPolicy.Calculate(
            row.Steps.SelectMany(step => step.Ingredients).Select(ingredient => new RecipeNutritionIngredient(
                ingredient.Amount, ingredient.ProductBaseAmount,
                new RecipeNutritionValues(ingredient.ProductCaloriesPerBase, ingredient.ProductProteinsPerBase, ingredient.ProductFatsPerBase,
                    ingredient.ProductCarbsPerBase, ingredient.ProductFiberPerBase, ingredient.ProductAlcoholPerBase),
                ingredient.NestedRecipeServings,
                new RecipeNutritionValues(ingredient.NestedRecipeTotalCalories, ingredient.NestedRecipeTotalProteins, ingredient.NestedRecipeTotalFats,
                    ingredient.NestedRecipeTotalCarbs, ingredient.NestedRecipeTotalFiber, ingredient.NestedRecipeTotalAlcohol))), stored);
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

}
