using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WeightEntries.Common;

public interface IWeightEntryWriteRepository {
    Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default);

    Task<WeightEntry?> GetByIdAsync(
        WeightEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<WeightEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
