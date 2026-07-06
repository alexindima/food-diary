using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Exercises.Models;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Services;

internal sealed class ExerciseEntryReadService(IExerciseEntryReadModelRepository exerciseEntryRepository) : IExerciseEntryReadService {
    public async Task<IReadOnlyList<ExerciseEntryModel>> GetEntriesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        IReadOnlyList<ExerciseEntryReadModel> entries = await exerciseEntryRepository
            .GetByDateRangeReadModelsAsync(userId, dateFrom, dateTo, cancellationToken)
            .ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }
}
