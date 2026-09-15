using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Common;

public interface ICycleWriteRepository {
    Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default);

    Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default);

    Task DeleteAsync(CycleProfile profile, CancellationToken cancellationToken = default);

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
}
