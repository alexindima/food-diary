using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.MealPlans;

internal sealed class MealPlanRepository(FoodDiaryDbContext context) : IMealPlanRepository {
    public async Task<MealPlan> AddAsync(MealPlan plan, CancellationToken cancellationToken) {
        context.Set<MealPlan>().Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async Task<MealPlan?> GetByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default) {
        var query = context.Set<MealPlan>().AsNoTracking();

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

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MealPlan>> GetCuratedAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default) {
        var query = context.Set<MealPlan>()
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meals)
            .Where(p => p.IsCurated)
            .AsSplitQuery();

        if (dietType.HasValue) {
            query = query.Where(p => p.DietType == dietType.Value);
        }

        return await query.OrderBy(p => p.DietType).ThenBy(p => p.Name).ToListAsync(cancellationToken);
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
            .ToListAsync(cancellationToken);
    }
}
