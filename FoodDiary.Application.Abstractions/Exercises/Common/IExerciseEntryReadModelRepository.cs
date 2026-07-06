using FoodDiary.Application.Abstractions.Exercises.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Exercises.Common;

public interface IExerciseEntryReadModelRepository {
    Task<IReadOnlyList<ExerciseEntryReadModel>> GetByDateRangeReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}