using FoodDiary.Mediator;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Exercises.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Exercises.Application.Queries.GetExerciseEntries;

public sealed class GetExerciseEntriesQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetExerciseEntriesQuery, Result<IReadOnlyList<ExerciseEntryModel>>> {
    public async Task<Result<IReadOnlyList<ExerciseEntryModel>>> Handle(
        GetExerciseEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ExerciseEntryModel>>(userIdResult);
        }

        IReadOnlyList<ExerciseEntryModel> models = await sender.Send(new ReadExerciseEntriesQuery(userIdResult.Value, query.DateFrom, query.DateTo), cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ExerciseEntryModel>>(models);
    }
}
