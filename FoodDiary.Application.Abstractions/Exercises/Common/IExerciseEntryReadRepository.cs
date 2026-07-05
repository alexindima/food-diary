using FoodDiary.Application.Abstractions.Exercises.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Exercises.Common;

public interface IExerciseEntryReadRepository {
    Task<ExerciseEntry?> GetByIdAsync(
        ExerciseEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExerciseEntry>> GetByDateRangeAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<ExerciseEntryReadModel>> GetByDateRangeReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<ExerciseEntry> entries = await GetByDateRangeAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(ToReadModel)];
    }

    Task<double> GetTotalCaloriesBurnedAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    private static ExerciseEntryReadModel ToReadModel(ExerciseEntry entry) {
        return new ExerciseEntryReadModel(
            entry.Id.Value,
            entry.Date,
            entry.ExerciseType.ToString(),
            entry.Name,
            entry.DurationMinutes,
            entry.CaloriesBurned,
            entry.Notes);
    }
}
