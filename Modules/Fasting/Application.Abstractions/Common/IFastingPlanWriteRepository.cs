using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingPlanWriteRepository {
    Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task AddAsync(FastingPlan plan, CancellationToken cancellationToken = default);

    Task UpdateAsync(FastingPlan plan, CancellationToken cancellationToken = default);
}
