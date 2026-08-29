using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingPlanReadRepository {
    Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingPlan>> GetByUserAsync(
        UserId userId,
        FastingPlanType? type = null,
        FastingPlanStatus? status = null,
        CancellationToken cancellationToken = default);
}
