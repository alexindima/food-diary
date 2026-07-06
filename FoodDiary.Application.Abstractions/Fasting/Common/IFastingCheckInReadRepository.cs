using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingCheckInReadRepository {
    Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingCheckInReadModel>> GetByOccurrenceIdReadModelsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default);
}
