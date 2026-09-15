using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Abstractions.Common;

public interface IHydrationIntervalReadModelRepository {
    Task<long> GetTotalAsync(UserId userId, DateTime startUtc, DateTime endExclusiveUtc, CancellationToken cancellationToken = default);
}
