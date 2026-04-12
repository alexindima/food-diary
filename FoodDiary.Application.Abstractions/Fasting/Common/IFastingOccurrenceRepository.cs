using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Common;

public interface IFastingOccurrenceRepository {
    Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);
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
    Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default);
    Task UpdateAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default);
}
