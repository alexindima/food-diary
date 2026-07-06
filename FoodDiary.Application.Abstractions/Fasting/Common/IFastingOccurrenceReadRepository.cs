using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingOccurrenceReadRepository {
    Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    async Task<FastingOccurrenceReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) {
        FastingOccurrence? occurrence = await GetCurrentAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return occurrence is null ? null : ToReadModel(occurrence);
    }

    Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingOccurrence>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(
        FastingPlanId planId,
        bool includeCompleted = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(
        UserId userId,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<FastingOccurrenceReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FastingOccurrence> occurrences = await GetByUserAsync(userId, from, to, status, cancellationToken).ConfigureAwait(false);
        return [.. occurrences.Select(ToReadModel)];
    }

    Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default);

    async Task<(IReadOnlyList<FastingOccurrenceReadModel> Items, int TotalItems)> GetPagedByUserReadModelsAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default) {
        (IReadOnlyList<FastingOccurrence> items, int totalItems) = await GetPagedByUserAsync(
            userId,
            page,
            limit,
            from,
            to,
            status,
            cancellationToken).ConfigureAwait(false);

        return ([.. items.Select(ToReadModel)], totalItems);
    }

    private static FastingOccurrenceReadModel ToReadModel(FastingOccurrence occurrence) =>
        new(
            occurrence.Id,
            occurrence.PlanId,
            ToReadModel(occurrence.Plan),
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

    private static FastingPlanReadModel? ToReadModel(FastingPlan? plan) =>
        plan is null
            ? null
            : new FastingPlanReadModel(
                plan.Id,
                plan.UserId,
                plan.Type,
                plan.Status,
                plan.Protocol,
                plan.Title,
                plan.StartedAtUtc,
                plan.StoppedAtUtc,
                plan.IntermittentFastHours,
                plan.IntermittentEatingWindowHours,
                plan.ExtendedTargetHours,
                plan.CyclicFastDays,
                plan.CyclicEatDays,
                plan.CyclicEatDayFastHours,
                plan.CyclicEatDayEatingWindowHours,
                plan.CyclicAnchorDateUtc,
                plan.CyclicNextPhaseDateUtc);
}
