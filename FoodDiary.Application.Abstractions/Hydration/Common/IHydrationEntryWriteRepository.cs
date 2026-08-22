using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public interface IHydrationEntryWriteRepository {
    Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default);

    Task<HydrationEntry?> GetByIdForUpdateAsync(
        HydrationEntryId id,
        CancellationToken cancellationToken = default);
}
