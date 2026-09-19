using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;

namespace FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;

public interface IDailyAdviceWriteRepository {
    Task<IReadOnlyList<DailyAdvice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyAdvice>> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    void RemoveRange(IReadOnlyList<DailyAdvice> advices);
    Task AddRangeAsync(IReadOnlyList<DailyAdvice> advices, CancellationToken cancellationToken = default);
}
