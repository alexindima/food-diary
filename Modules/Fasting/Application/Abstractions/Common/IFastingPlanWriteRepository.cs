using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingPlanWriteRepository {
    Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task AddAsync(FastingPlan plan, CancellationToken cancellationToken = default);

    Task UpdateAsync(FastingPlan plan, CancellationToken cancellationToken = default);
}
