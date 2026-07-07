using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class FastingOccurrenceRepository(FoodDiaryDbContext context) : IFastingOccurrenceRepository {
    private static readonly Expression<Func<FastingOccurrence, FastingOccurrenceReadModel>> ReadModelProjection =
        occurrence => new FastingOccurrenceReadModel(
            occurrence.Id,
            occurrence.PlanId,
            new FastingPlanReadModel(
                occurrence.Plan.Id,
                occurrence.Plan.UserId,
                occurrence.Plan.Type,
                occurrence.Plan.Status,
                occurrence.Plan.Protocol,
                occurrence.Plan.Title,
                occurrence.Plan.StartedAtUtc,
                occurrence.Plan.StoppedAtUtc,
                occurrence.Plan.IntermittentFastHours,
                occurrence.Plan.IntermittentEatingWindowHours,
                occurrence.Plan.ExtendedTargetHours,
                occurrence.Plan.CyclicFastDays,
                occurrence.Plan.CyclicEatDays,
                occurrence.Plan.CyclicEatDayFastHours,
                occurrence.Plan.CyclicEatDayEatingWindowHours,
                occurrence.Plan.CyclicAnchorDateUtc,
                occurrence.Plan.CyclicNextPhaseDateUtc),
            occurrence.UserId,
            occurrence.Kind,
            occurrence.Status,
            occurrence.SequenceNumber,
            occurrence.ScheduledForUtc,
            occurrence.StartedAtUtc,
            occurrence.EndedAtUtc,
            occurrence.InitialTargetHours,
            occurrence.AddedTargetHours,
            occurrence.Notes,
            occurrence.CheckInAtUtc,
            occurrence.HungerLevel,
            occurrence.EnergyLevel,
            occurrence.MoodLevel,
            occurrence.Symptoms,
            occurrence.CheckInNotes);

    public async Task<IReadOnlyList<FastingOccurrence>> GetActiveAsync(CancellationToken cancellationToken = default) {
        return await context.FastingOccurrences
            .AsNoTracking()
            .Include(occurrence => occurrence.Plan)
            .Include(occurrence => occurrence.User)
            .Where(occurrence => occurrence.Status == FastingOccurrenceStatus.Active)
            .OrderBy(occurrence => occurrence.StartedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) {
        IIncludableQueryable<FastingOccurrence, FastingPlan> query = (asTracking
            ? context.FastingOccurrences.AsQueryable()
            : context.FastingOccurrences.AsNoTracking())
            .Include(occurrence => occurrence.Plan);

        return await query
            .Where(occurrence => occurrence.UserId == userId && occurrence.Status == FastingOccurrenceStatus.Active)
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FastingOccurrenceReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.FastingOccurrences
            .AsNoTracking()
            .Where(occurrence => occurrence.UserId == userId && occurrence.Status == FastingOccurrenceStatus.Active)
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .Select(ReadModelProjection)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FastingOccurrence?> GetByIdAsync(
        FastingOccurrenceId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingOccurrence> query = (asTracking
            ? context.FastingOccurrences
            : context.FastingOccurrences.AsNoTracking())
            .Include(occurrence => occurrence.Plan);

        return await query.FirstOrDefaultAsync(occurrence => occurrence.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(
        FastingPlanId planId,
        bool includeCompleted = true,
        CancellationToken cancellationToken = default) {
        IQueryable<FastingOccurrence> query = context.FastingOccurrences
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
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(
        UserId userId,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        IQueryable<FastingOccurrence> query = BuildByUserQuery(userId, from, to, status);

        return await query
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingOccurrenceReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        return await BuildByUserQuery(userId, from, to, status)
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .Select(ReadModelProjection)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        int normalizedPage = Math.Max(page, 1);
        int normalizedLimit = Math.Max(limit, 1);
        IQueryable<FastingOccurrence> query = BuildByUserQuery(userId, from, to, status);
        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<FastingOccurrence> items = await query
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .Skip((normalizedPage - 1) * normalizedLimit)
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, totalItems);
    }

    public async Task<(IReadOnlyList<FastingOccurrenceReadModel> Items, int TotalItems)> GetPagedByUserReadModelsAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        int normalizedPage = Math.Max(page, 1);
        int normalizedLimit = Math.Max(limit, 1);
        IQueryable<FastingOccurrence> query = BuildByUserQuery(userId, from, to, status);
        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<FastingOccurrenceReadModel> items = await query
            .OrderByDescending(occurrence => occurrence.StartedAtUtc)
            .Skip((normalizedPage - 1) * normalizedLimit)
            .Take(normalizedLimit)
            .Select(ReadModelProjection)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, totalItems);
    }

    private IQueryable<FastingOccurrence> BuildByUserQuery(
        UserId userId,
        DateTime? from,
        DateTime? to,
        FastingOccurrenceStatus? status) {
        IQueryable<FastingOccurrence> query = context.FastingOccurrences
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
        await context.FastingOccurrences.AddAsync(occurrence, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default) {
        context.FastingOccurrences.Update(occurrence);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
