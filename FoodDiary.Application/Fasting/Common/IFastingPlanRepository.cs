using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Common;

public interface IFastingPlanRepository {
    Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);
    Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FastingPlan>> GetByUserAsync(
        UserId userId,
        FastingPlanType? type = null,
        FastingPlanStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(FastingPlan plan, CancellationToken cancellationToken = default);
    Task UpdateAsync(FastingPlan plan, CancellationToken cancellationToken = default);
}
