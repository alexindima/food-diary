using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Abstractions.DailyAdvices.Common;

public interface IDailyAdviceRepository {
    Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default);
}
