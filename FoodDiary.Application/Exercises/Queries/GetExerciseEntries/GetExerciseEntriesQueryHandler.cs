using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

public sealed class GetExerciseEntriesQueryHandler(IExerciseEntryReadRepository repository)
    : IQueryHandler<GetExerciseEntriesQuery, Result<IReadOnlyList<ExerciseEntryModel>>> {
    public async Task<Result<IReadOnlyList<ExerciseEntryModel>>> Handle(
        GetExerciseEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<ExerciseEntryModel>>(userIdResult.Error);
        }

        IReadOnlyList<ExerciseEntry> entries = await repository.GetByDateRangeAsync(
            userIdResult.Value, query.DateFrom, query.DateTo, cancellationToken).ConfigureAwait(false);

        var models = entries.Select(e => e.ToModel()).ToList();
        return Result.Success<IReadOnlyList<ExerciseEntryModel>>(models);
    }
}
