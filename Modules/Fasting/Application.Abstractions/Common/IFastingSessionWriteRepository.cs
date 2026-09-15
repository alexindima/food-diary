using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingSessionWriteRepository {
    Task<FastingSession?> GetByIdAsync(FastingSessionId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingSession> AddAsync(FastingSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(FastingSession session, CancellationToken cancellationToken = default);
}
