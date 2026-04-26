using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class FastingCheckInRepository(FoodDiaryDbContext context) : IFastingCheckInRepository {
    public async Task AddAsync(FastingCheckIn checkIn, CancellationToken cancellationToken = default) {
        await context.FastingCheckIns.AddAsync(checkIn, cancellationToken);
    }

    public async Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default) {
        if (occurrenceIds.Count == 0) {
            return [];
        }

        return await context.FastingCheckIns
            .AsNoTracking()
            .Where(x => occurrenceIds.Contains(x.OccurrenceId))
            .OrderByDescending(x => x.CheckedInAtUtc)
            .ToListAsync(cancellationToken);
    }
}
