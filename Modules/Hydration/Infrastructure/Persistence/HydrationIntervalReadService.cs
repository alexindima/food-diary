using FoodDiary.Application.Hydration.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Hydration.Infrastructure.Persistence;

public sealed class HydrationIntervalReadService(DbSet<HydrationEntry> entries) : IHydrationIntervalReadService {
    public Task<long> GetTotalAsync(UserId userId, DateTime startUtc, DateTime endExclusiveUtc, CancellationToken cancellationToken = default) {
        if (startUtc.Kind != DateTimeKind.Utc || endExclusiveUtc.Kind != DateTimeKind.Utc || endExclusiveUtc < startUtc) {
            throw new ArgumentException("An ordered UTC interval is required.", nameof(startUtc));
        }
        return entries.AsNoTracking().Where(entry => entry.UserId == userId && entry.Timestamp >= startUtc && entry.Timestamp < endExclusiveUtc)
            .SumAsync(entry => (long)entry.AmountMl, cancellationToken);
    }
}
