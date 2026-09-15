using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Application.Abstractions.Common;
using FoodDiary.Modules.Fasting.Application.Abstractions.Models;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Fasting.Infrastructure.Persistence;

public sealed class FastingCheckInRepository(DbSet<FastingCheckIn> entries) : IFastingCheckInRepository {
    public async Task AddAsync(FastingCheckIn checkIn, CancellationToken cancellationToken = default) {
        await entries.AddAsync(checkIn, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default) {
        if (occurrenceIds.Count == 0) {
            return [];
        }

        return await entries
            .AsNoTracking()
            .Where(x => occurrenceIds.Contains(x.OccurrenceId))
            .OrderByDescending(x => x.CheckedInAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingCheckInReadModel>> GetByOccurrenceIdReadModelsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default) {
        if (occurrenceIds.Count == 0) {
            return [];
        }

        return await entries
            .AsNoTracking()
            .Where(checkIn => occurrenceIds.Contains(checkIn.OccurrenceId))
            .OrderByDescending(checkIn => checkIn.CheckedInAtUtc)
            .Select(checkIn => new FastingCheckInReadModel(
                checkIn.Id,
                checkIn.OccurrenceId,
                checkIn.CheckedInAtUtc,
                checkIn.HungerLevel,
                checkIn.EnergyLevel,
                checkIn.MoodLevel,
                checkIn.Symptoms,
                checkIn.Notes))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
