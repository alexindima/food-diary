using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Cycles.Common;

public interface ICycleRepository {
    Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default);

    Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default);

    Task<CycleProfile?> GetByIdAsync(
        CycleProfileId id,
        UserId userId,
        bool includeDetails = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<CycleProfile?> GetCurrentAsync(
        UserId userId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CycleProfile>> GetByUserAsync(
        UserId userId,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);
}
