using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IHydrationEntryRepository
{
    Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default);
    Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);

    Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default);
}
