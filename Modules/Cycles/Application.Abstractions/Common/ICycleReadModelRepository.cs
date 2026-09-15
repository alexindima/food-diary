using FoodDiary.Modules.Cycles.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Common;

public interface ICycleReadModelRepository {
    Task<CycleProfileReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
