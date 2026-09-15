using FoodDiary.Modules.Exercises.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Exercises.Application.Abstractions.Common;

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

    Task<double> GetTotalCaloriesBurnedAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
