using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Common;

public interface IExerciseEntryReadService {
    Task<IReadOnlyList<ExerciseEntryModel>> GetEntriesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken);
}
