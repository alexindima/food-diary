using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingPlanReadRepository {
    Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingPlan>> GetByUserAsync(
        UserId userId,
        FastingPlanType? type = null,
        FastingPlanStatus? status = null,
        CancellationToken cancellationToken = default);
}
