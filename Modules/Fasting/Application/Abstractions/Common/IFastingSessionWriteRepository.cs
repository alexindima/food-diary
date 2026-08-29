using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingSessionWriteRepository {
    Task<FastingSession?> GetByIdAsync(FastingSessionId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingSession> AddAsync(FastingSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(FastingSession session, CancellationToken cancellationToken = default);
}
