using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingOccurrenceWriteRepository {
    Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task AddAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default);

    Task UpdateAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default);
}
