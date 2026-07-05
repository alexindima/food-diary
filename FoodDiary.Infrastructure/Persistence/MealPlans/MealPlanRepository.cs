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
                        .ThenInclude(m => m.Recipe)
                            .ThenInclude(r => r.Steps)
                                .ThenInclude(s => s.Ingredients)
                                    .ThenInclude(i => i.Product)
                .AsSplitQuery();
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
    }

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
                            .Select(m => new MealPlanMealReadModel(
                                m.Id.Value,
                                m.MealType.ToString(),
                                m.RecipeId.Value,
                                m.Recipe.Name,
                                m.Servings,
                                m.Recipe.Servings > 0 ? m.Recipe.Servings : 1,
                                m.Recipe.TotalCalories,
                                m.Recipe.TotalProteins,
                                m.Recipe.TotalFats,
                                m.Recipe.TotalCarbs))
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

        return await query
            .OrderBy(p => p.DietType)
            .ThenBy(p => p.Name)
            .Select(p => new MealPlanSummaryReadModel(
                p.Id.Value,
                p.Name,
                p.Description,
                p.DietType.ToString(),
                p.DurationDays,
                p.TargetCaloriesPerDay,
                p.IsCurated,
                p.Days
                    .SelectMany(d => d.Meals)
                    .Select(m => m.RecipeId)
                    .Distinct()
                    .Count()))
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
        return await context.Set<MealPlan>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedOnUtc)
            .Select(p => new MealPlanSummaryReadModel(
                p.Id.Value,
                p.Name,
                p.Description,
                p.DietType.ToString(),
                p.DurationDays,
                p.TargetCaloriesPerDay,
                p.IsCurated,
                p.Days
                    .SelectMany(d => d.Meals)
                    .Select(m => m.RecipeId)
                    .Distinct()
                    .Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
