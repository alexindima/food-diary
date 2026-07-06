using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Cycles.Common;

public interface ICycleReadModelRepository {
    Task<CycleProfileReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}