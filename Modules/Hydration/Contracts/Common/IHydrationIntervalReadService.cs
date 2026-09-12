using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Common;

public interface IHydrationIntervalReadService {
    Task<long> GetTotalAsync(UserId userId, DateTime startUtc, DateTime endExclusiveUtc, CancellationToken cancellationToken = default);
}
