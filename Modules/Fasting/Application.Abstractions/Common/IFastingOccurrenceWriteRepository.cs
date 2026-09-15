using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingOccurrenceWriteRepository {
    Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task AddAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default);

    Task UpdateAsync(FastingOccurrence occurrence, CancellationToken cancellationToken = default);
}
