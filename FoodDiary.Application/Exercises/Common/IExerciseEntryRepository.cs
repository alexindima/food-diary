using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Common;

public interface IExerciseEntryRepository {
    Task<ExerciseEntry> AddAsync(ExerciseEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(ExerciseEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(ExerciseEntry entry, CancellationToken cancellationToken = default);

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

    Task<double> GetTotalCaloriesBurnedAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
