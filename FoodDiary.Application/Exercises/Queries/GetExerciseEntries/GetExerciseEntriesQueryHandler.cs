using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Queries.GetExerciseEntries;

public sealed class GetExerciseEntriesQueryHandler(IExerciseEntryReadService exerciseEntryReadService)
    : IQueryHandler<GetExerciseEntriesQuery, Result<IReadOnlyList<ExerciseEntryModel>>> {
    public async Task<Result<IReadOnlyList<ExerciseEntryModel>>> Handle(
        GetExerciseEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<ExerciseEntryModel>>(userIdResult);
        }

        IReadOnlyList<ExerciseEntryModel> models = await exerciseEntryReadService.GetEntriesAsync(
            userIdResult.Value,
            query.DateFrom,
            query.DateTo,
            cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ExerciseEntryModel>>(models);
    }
}
