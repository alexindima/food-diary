using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Abstractions.DailyAdvices.Common;

public interface IDailyAdviceReadRepository {
    Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default);
}