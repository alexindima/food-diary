using FoodDiary.Application.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class FastingPlanRepository(FoodDiaryDbContext context) : IFastingPlanRepository {
    public async Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) {
        var query = asTracking
            ? context.FastingPlans.AsQueryable()
            : context.FastingPlans.AsNoTracking();

        return await query
            .Where(plan => plan.UserId == userId && plan.Status == FastingPlanStatus.Active)
            .OrderByDescending(plan => plan.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FastingPlan?> GetByIdAsync(
        FastingPlanId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingPlan> query = asTracking
            ? context.FastingPlans
            : context.FastingPlans.AsNoTracking();

        return await query.FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FastingPlan>> GetByUserAsync(
        UserId userId,
        FastingPlanType? type = null,
        FastingPlanStatus? status = null,
        CancellationToken cancellationToken = default) {
        var query = context.FastingPlans
            .AsNoTracking()
            .Where(plan => plan.UserId == userId);

        if (type.HasValue) {
            query = query.Where(plan => plan.Type == type.Value);
        }

        if (status.HasValue) {
            query = query.Where(plan => plan.Status == status.Value);
        }

        return await query
            .OrderByDescending(plan => plan.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FastingPlan plan, CancellationToken cancellationToken = default) {
        await context.FastingPlans.AddAsync(plan, cancellationToken);
    }

    public async Task UpdateAsync(FastingPlan plan, CancellationToken cancellationToken = default) {
        context.FastingPlans.Update(plan);
        await Task.CompletedTask;
    }
}
