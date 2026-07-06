using FoodDiary.Application.Abstractions.DailyAdvices.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Abstractions.DailyAdvices.Common;

public interface IDailyAdviceReadRepository {
    Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyAdviceReadModel>> GetByLocaleReadModelsAsync(
        string locale,
        CancellationToken cancellationToken = default);
}
