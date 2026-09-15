using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingCheckInReadRepository {
    Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default);
}
