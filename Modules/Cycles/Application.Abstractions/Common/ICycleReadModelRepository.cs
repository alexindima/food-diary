using FoodDiary.Modules.Cycles.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Common;

public interface ICycleReadModelRepository {
    Task<CycleProfileReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
