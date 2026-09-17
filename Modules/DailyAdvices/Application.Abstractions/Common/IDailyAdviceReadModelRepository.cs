using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;

namespace FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;

public interface IDailyAdviceReadModelRepository {
    Task<IReadOnlyList<DailyAdviceReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyAdviceReadModel>> GetByLocaleReadModelsAsync(
        string locale,
        CancellationToken cancellationToken = default);
}
