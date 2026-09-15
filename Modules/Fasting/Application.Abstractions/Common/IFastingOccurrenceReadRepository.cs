using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Fasting.Application.Abstractions.Models;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingOccurrenceReadRepository {
    Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingActiveOccurrenceModel>> GetActiveAsync(CancellationToken cancellationToken = default);

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
}
