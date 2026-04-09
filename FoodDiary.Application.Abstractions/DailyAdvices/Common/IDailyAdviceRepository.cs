using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.DailyAdvices.Common;

public interface IDailyAdviceRepository {
    Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default);
}
