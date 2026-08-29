using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingCheckInReadModelRepository {
    Task<IReadOnlyList<FastingCheckInReadModel>> GetByOccurrenceIdReadModelsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default);
}
