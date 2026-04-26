using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class FastingOccurrenceRepository(FoodDiaryDbContext context) : IFastingOccurrenceRepository {
    public async Task<IReadOnlyList<FastingOccurrence>> GetActiveAsync(CancellationToken cancellationToken = default) {
        return await context.FastingOccurrences
            .AsNoTracking()
            .Include(occurrence => occurrence.Plan)
            .Include(occurrence => occurrence.User)
            .Where(occurrence => occurrence.Status == FastingOccurrenceStatus.Active)
            .OrderBy(occurrence => occurrence.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) {
        var query = (asTracking
            ? context.FastingOccurrences.AsQueryable()
            : context.FastingOccurrences.AsNoTracking())
            .Include(occurrence => occurrence.Plan);

        return await query
            .Where(occurrence => occurrence.UserId == userId && occurrence.Status == FastingOccurrenceStatus.Active)
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FastingOccurrence?> GetByIdAsync(
        FastingOccurrenceId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingOccurrence> query = (asTracking
            ? context.FastingOccurrences
            : context.FastingOccurrences.AsNoTracking())
            .Include(occurrence => occurrence.Plan);

        return await query.FirstOrDefaultAsync(occurrence => occurrence.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(
        FastingPlanId planId,
        bool includeCompleted = true,
        CancellationToken cancellationToken = default) {
        var query = context.FastingOccurrences
            .Include(occurrence => occurrence.Plan)
            .AsNoTracking()
            .Where(occurrence => occurrence.PlanId == planId);

        if (!includeCompleted) {
            query = query.Where(occurrence =>
                occurrence.Status == FastingOccurrenceStatus.Active ||
                occurrence.Status == FastingOccurrenceStatus.Scheduled ||
                occurrence.Status == FastingOccurrenceStatus.Postponed);
        }

        return await query
            .OrderBy(occurrence => occurrence.SequenceNumber)
            .ThenBy(occurrence => occurrence.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(
        UserId userId,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        var query = BuildByUserQuery(userId, from, to, status);

        return await query
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        var normalizedPage = Math.Max(page, 1);
        var normalizedLimit = Math.Max(limit, 1);
        var query = BuildByUserQuery(userId, from, to, status);
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .Skip((normalizedPage - 1) * normalizedLimit)
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    private IQueryable<FastingOccurrence> BuildByUserQuery(
        UserId userId,
        DateTime? from,
        DateTime? to,
        FastingOccurrenceStatus? status) {
        var query = context.FastingOccurrences
            .Include(occurrence => occurrence.Plan)
            .AsNoTracking()
            .Where(occurrence => occurrence.UserId == userId);

        if (from.HasValue) {
            query = query.Where(occurrence => occurrence.StartedAtUtc >= from.Value);
        }

        if (to.HasValue) {
            query = query.Where(occurrence => occurrence.StartedAtUtc <= to.Value);
        }

        if (status.HasValue) {
            query = query.Where(occurrence => occurrence.Status == status.Value);
        }

        return query;
    }

    public async Task AddAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default) {
        await context.FastingOccurrences.AddAsync(occurrence, cancellationToken);
    }

    public async Task UpdateAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default) {
        context.FastingOccurrences.Update(occurrence);
        await Task.CompletedTask;
    }
}
