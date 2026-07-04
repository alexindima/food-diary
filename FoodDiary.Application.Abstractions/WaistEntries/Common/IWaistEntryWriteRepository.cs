using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WaistEntries.Common;

public interface IWaistEntryWriteRepository {
    Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default);

    Task<WaistEntry?> GetByIdAsync(
        WaistEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<WaistEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
