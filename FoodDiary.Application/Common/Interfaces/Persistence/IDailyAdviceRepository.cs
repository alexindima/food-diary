using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IDailyAdviceRepository {
    Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default);
}
