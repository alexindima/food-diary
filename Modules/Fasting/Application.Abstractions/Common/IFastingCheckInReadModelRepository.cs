using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Application.Abstractions.Models;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingCheckInReadModelRepository {
    Task<IReadOnlyList<FastingCheckInReadModel>> GetByOccurrenceIdReadModelsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default);
}
