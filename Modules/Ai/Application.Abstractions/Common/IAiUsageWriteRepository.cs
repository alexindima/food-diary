using FoodDiary.Modules.Ai.Domain.Entities;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IAiUsageWriteRepository {
    Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default);
}
