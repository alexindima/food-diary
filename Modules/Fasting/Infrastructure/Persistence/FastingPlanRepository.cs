using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Fasting.Infrastructure.Persistence;

public sealed class FastingPlanRepository(DbSet<FastingPlan> entries) : IFastingPlanRepository {
    public async Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingPlan> query = asTracking
            ? entries.AsQueryable()
            : entries.AsNoTracking();

        return await query
            .Where(plan => plan.UserId == userId && plan.Status == FastingPlanStatus.Active)
            .OrderByDescending(plan => plan.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FastingPlan?> GetByIdAsync(
        FastingPlanId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingPlan> query = asTracking
            ? entries
            : entries.AsNoTracking();

        return await query.FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingPlan>> GetByUserAsync(
        UserId userId,
        FastingPlanType? type = null,
        FastingPlanStatus? status = null,
        CancellationToken cancellationToken = default) {
        IQueryable<FastingPlan> query = entries
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
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(FastingPlan plan, CancellationToken cancellationToken = default) {
        await entries.AddAsync(plan, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(FastingPlan plan, CancellationToken cancellationToken = default) {
        entries.Update(plan);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
