using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public interface IHydrationGoalService {
    Task<Result<double?>> GetCurrentGoalAsync(UserId userId, CancellationToken cancellationToken = default);
}
