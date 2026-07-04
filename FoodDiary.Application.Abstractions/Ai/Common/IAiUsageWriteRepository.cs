using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiUsageWriteRepository : IAiUsageReadRepository {
    Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default);
}
