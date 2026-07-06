using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingCheckInReadRepository {
    Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<FastingCheckInReadModel>> GetByOccurrenceIdReadModelsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FastingCheckIn> checkIns = await GetByOccurrenceIdsAsync(occurrenceIds, cancellationToken).ConfigureAwait(false);
        return [.. checkIns.Select(static checkIn => new FastingCheckInReadModel(
            checkIn.Id,
            checkIn.OccurrenceId,
            checkIn.CheckedInAtUtc,
            checkIn.HungerLevel,
            checkIn.EnergyLevel,
            checkIn.MoodLevel,
            checkIn.Symptoms,
            checkIn.Notes))];
    }
}
