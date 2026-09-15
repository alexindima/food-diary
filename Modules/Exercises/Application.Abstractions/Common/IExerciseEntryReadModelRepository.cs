using FoodDiary.Modules.Exercises.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Exercises.Application.Abstractions.Common;

public interface IExerciseEntryReadModelRepository {
    Task<IReadOnlyList<ExerciseEntryReadModel>> GetByDateRangeReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
