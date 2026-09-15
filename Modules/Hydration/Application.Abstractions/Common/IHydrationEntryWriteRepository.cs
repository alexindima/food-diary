using FoodDiary.Modules.Hydration.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;

namespace FoodDiary.Modules.Hydration.Application.Abstractions.Common;

public interface IHydrationEntryWriteRepository {
    Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default);

    Task<HydrationEntry?> GetByIdForUpdateAsync(
        HydrationEntryId id,
        CancellationToken cancellationToken = default);
}
