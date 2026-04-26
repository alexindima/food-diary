using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;

namespace FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

public class GetExerciseEntriesQueryHandler(IExerciseEntryRepository repository)
    : IQueryHandler<GetExerciseEntriesQuery, Result<IReadOnlyList<ExerciseEntryModel>>> {
    public async Task<Result<IReadOnlyList<ExerciseEntryModel>>> Handle(
        GetExerciseEntriesQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<ExerciseEntryModel>>(userIdResult.Error);
        }

        var entries = await repository.GetByDateRangeAsync(
            userIdResult.Value, query.DateFrom, query.DateTo, cancellationToken);

        var models = entries.Select(e => e.ToModel()).ToList();
        return Result.Success<IReadOnlyList<ExerciseEntryModel>>(models);
    }
}
