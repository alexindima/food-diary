using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.MealPlans;

internal sealed class MealPlanRepository(FoodDiaryDbContext context) : IMealPlanRepository {
    public Task<MealPlan> AddAsync(MealPlan plan, CancellationToken cancellationToken = default) {
        context.Set<MealPlan>().Add(plan);
        return Task.FromResult(plan);
    }

    public async Task<MealPlan?> GetByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default) {
        IQueryable<MealPlan> query = context.Set<MealPlan>().AsNoTracking();

        if (includeDays) {
            query = query
                .Include(p => p.Days)
                    .ThenInclude(d => d.Meals)
                .AsSplitQuery();
        }

        MealPlan? result = await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
        if (includeDays && result is not null) { await LoadRecipeSnapshotsAsync(result, cancellationToken).ConfigureAwait(false); }
        return result;
    }

    public Task<MealPlan?> GetCuratedByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default) =>
        GetFilteredByIdAsync(id, static plan => plan.IsCurated, includeDays, cancellationToken);

    public Task<MealPlan?> GetAccessibleByIdAsync(
        MealPlanId id,
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default) =>
        GetFilteredByIdAsync(
            id,
            plan => plan.IsCurated || plan.UserId == userId,
            includeDays,
            cancellationToken);

    public async Task<MealPlanReadModel?> GetReadModelByIdAsync(
        MealPlanId id,
        CancellationToken cancellationToken = default) {
        return await context.Set<MealPlan>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new MealPlanReadModel(
                p.Id.Value,
                p.UserId == null ? null : p.UserId.Value.Value,
                p.Name,
                p.Description,
                p.DietType.ToString(),
                p.DurationDays,
                p.TargetCaloriesPerDay,
                p.IsCurated,
                p.Days
                    .OrderBy(d => d.DayNumber)
                    .Select(d => new MealPlanDayReadModel(
                        d.Id.Value,
                        d.DayNumber,
                        d.Meals
                            .OrderBy(m => m.MealType)
                            .Join(context.Recipes.AsNoTracking(), m => m.RecipeId, recipe => recipe.Id, (m, recipe) => new MealPlanMealReadModel(
                                m.Id.Value,
                                m.MealType.ToString(),
                                m.RecipeId.Value,
                                recipe.Name,
                                m.Servings,
                                recipe.Servings > 0 ? recipe.Servings : 1,
                                recipe.TotalCalories,
                                recipe.TotalProteins,
                                recipe.TotalFats,
                                recipe.TotalCarbs))
                            .ToList()))
                    .ToList()))
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MealPlan>> GetCuratedAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default) {
        IQueryable<MealPlan> query = context.Set<MealPlan>()
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meals)
            .Where(p => p.IsCurated)
            .AsSplitQuery();

        if (dietType.HasValue) {
            query = query.Where(p => p.DietType == dietType.Value);
        }

        return await query.OrderBy(p => p.DietType).ThenBy(p => p.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MealPlanSummaryReadModel>> GetCuratedSummaryReadModelsAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default) {
        IQueryable<MealPlan> query = context.Set<MealPlan>()
            .AsNoTracking()
            .Where(p => p.IsCurated);

        if (dietType.HasValue) {
            query = query.Where(p => p.DietType == dietType.Value);
        }

        return await ProjectSummaryReadModels(query
                .OrderBy(p => p.DietType)
                .ThenBy(p => p.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MealPlan>> GetByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.Set<MealPlan>()
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meals)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedOnUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MealPlanSummaryReadModel>> GetByUserSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await ProjectSummaryReadModels(context.Set<MealPlan>()
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static IQueryable<MealPlanSummaryReadModel> ProjectSummaryReadModels(IQueryable<MealPlan> query) =>
        query.Select(plan => new MealPlanSummaryReadModel(
            plan.Id.Value,
            plan.Name,
            plan.Description,
            plan.DietType.ToString(),
            plan.DurationDays,
            plan.TargetCaloriesPerDay,
            plan.IsCurated,
            plan.Days
                .SelectMany(day => day.Meals)
                .Select(meal => meal.RecipeId)
                .Distinct()
                .Count()));

    private async Task<MealPlan?> GetFilteredByIdAsync(
        MealPlanId id,
        System.Linq.Expressions.Expression<Func<MealPlan, bool>> accessPredicate,
        bool includeDays,
        CancellationToken cancellationToken) {
        IQueryable<MealPlan> query = context.Set<MealPlan>()
            .AsNoTracking()
            .Where(accessPredicate);

        if (includeDays) {
            query = query
                .Include(plan => plan.Days)
                    .ThenInclude(day => day.Meals)
                .AsSplitQuery();
        }

        MealPlan? result = await query.FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken).ConfigureAwait(false);
        if (includeDays && result is not null) { await LoadRecipeSnapshotsAsync(result, cancellationToken).ConfigureAwait(false); }
        return result;
    }

    private async Task LoadRecipeSnapshotsAsync(MealPlan plan, CancellationToken cancellationToken) {
        MealPlanMeal[] meals = [.. plan.Days.SelectMany(day => day.Meals)];
        RecipeId[] ids = [.. meals.Select(meal => meal.RecipeId).Distinct()];
        if (ids.Length == 0) { return; }

        var recipes = await context.Recipes.AsNoTracking()
            .Where(recipe => Enumerable.Contains(ids, recipe.Id))
            .Select(recipe => new { recipe.Id, recipe.Name, recipe.Servings, recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ingredients = await context.Recipes.AsNoTracking()
            .Where(recipe => Enumerable.Contains(ids, recipe.Id))
            .SelectMany(recipe => recipe.Steps.SelectMany(step => step.Ingredients)
                .Select(ingredient => new { RecipeId = recipe.Id, ingredient.ProductId, ingredient.Amount }))
            .Join(context.Products.AsNoTracking(), ingredient => ingredient.ProductId, product => (ProductId?)product.Id,
                (ingredient, product) => new {
                    ingredient.RecipeId,
                    Ingredient = new MealPlanRecipeIngredientSnapshot(product.Id, ingredient.Amount, product.Name, product.BaseUnit, product.Category),
                })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        ILookup<RecipeId, MealPlanRecipeIngredientSnapshot> byRecipe = ingredients.ToLookup(item => item.RecipeId, item => item.Ingredient);
        var snapshots = recipes.ToDictionary(recipe => recipe.Id,
            recipe => new MealPlanRecipeSnapshot(recipe.Id, recipe.Name, recipe.Servings, byRecipe[recipe.Id].ToArray(), recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs));
        foreach (MealPlanMeal meal in meals) {
            meal.SetRecipeSnapshot(snapshots.GetValueOrDefault(meal.RecipeId));
        }
    }
}
