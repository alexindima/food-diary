using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Cycles.Common;

public interface ICycleRepository {
    Task<Cycle> AddAsync(Cycle cycle, CancellationToken cancellationToken = default);

    Task UpdateAsync(Cycle cycle, CancellationToken cancellationToken = default);

    Task<Cycle?> GetByIdAsync(
        CycleId id,
        UserId userId,
        bool includeDays = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<Cycle?> GetLatestAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cycle>> GetByUserAsync(
        UserId userId,
        bool includeDays = false,
        CancellationToken cancellationToken = default);
}
