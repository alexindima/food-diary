using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Exercises.Common;

public interface IExerciseEntryWriteRepository {
    Task<ExerciseEntry> AddAsync(ExerciseEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(ExerciseEntry entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(ExerciseEntry entry, CancellationToken cancellationToken = default);

    Task<ExerciseEntry?> GetByIdAsync(
        ExerciseEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);
}
