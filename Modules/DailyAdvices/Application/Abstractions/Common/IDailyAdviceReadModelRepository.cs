using FoodDiary.Application.Abstractions.DailyAdvices.Models;

namespace FoodDiary.Application.Abstractions.DailyAdvices.Common;

public interface IDailyAdviceReadModelRepository {
    Task<IReadOnlyList<DailyAdviceReadModel>> GetByLocaleReadModelsAsync(
        string locale,
        CancellationToken cancellationToken = default);
}
