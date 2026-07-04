using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingSessionReadRepository {
    Task<FastingSession?> GetCurrentAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<FastingSession?> GetByIdAsync(FastingSessionId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingSession>> GetHistoryAsync(UserId userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<int> GetCompletedCountAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<int> GetCurrentStreakAsync(UserId userId, CancellationToken cancellationToken = default);
}
