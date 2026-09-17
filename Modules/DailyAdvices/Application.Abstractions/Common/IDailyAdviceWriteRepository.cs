using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;

namespace FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;

public interface IDailyAdviceWriteRepository {
    Task AddRangeAsync(IReadOnlyList<DailyAdvice> advices, CancellationToken cancellationToken = default);
}
