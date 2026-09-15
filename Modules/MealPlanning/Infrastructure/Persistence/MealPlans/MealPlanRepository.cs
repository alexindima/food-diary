using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.MealPlans;

internal sealed class MealPlanRepository(DbSet<MealPlan> plans, IMealPlanCompositionReader composition) : IMealPlanRepository {
    public Task<MealPlan> AddAsync(MealPlan plan, CancellationToken cancellationToken = default) {
        plans.Add(plan);
        return Task.FromResult(plan);
    }

    public async Task<MealPlan?> GetByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default) {
        IQueryable<MealPlan> query = plans.AsNoTracking();

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

    public Task<MealPlanReadModel?> GetReadModelByIdAsync(
        MealPlanId id, CancellationToken cancellationToken = default) =>
        composition.GetReadModelByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<MealPlan>> GetCuratedAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default) {
        IQueryable<MealPlan> query = plans
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
        IQueryable<MealPlan> query = plans
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
        return await plans
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
        return await ProjectSummaryReadModels(plans
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
        IQueryable<MealPlan> query = plans
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

        IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot> snapshots = await composition
            .GetRecipeSnapshotsAsync(ids, cancellationToken).ConfigureAwait(false);
        foreach (MealPlanMeal meal in meals) {
            meal.SetRecipeSnapshot(snapshots.GetValueOrDefault(meal.RecipeId));
        }
    }
}
