using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Services;

internal sealed class ExerciseEntryReadService(IExerciseEntryReadRepository exerciseEntryRepository) : IExerciseEntryReadService {
    public async Task<IReadOnlyList<ExerciseEntryModel>> GetEntriesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        IReadOnlyList<ExerciseEntry> entries = await exerciseEntryRepository
            .GetByDateRangeAsync(userId, dateFrom, dateTo, cancellationToken)
            .ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }
}
