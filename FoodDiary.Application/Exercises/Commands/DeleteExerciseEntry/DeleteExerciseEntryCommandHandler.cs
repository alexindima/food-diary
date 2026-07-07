using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;

public sealed class DeleteExerciseEntryCommandHandler(
    IExerciseEntryWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteExerciseEntryCommand, Result> {
    public async Task<Result> Handle(
        DeleteExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        Result<ExerciseEntryId> entryIdResult = RequiredIdParser.Parse(
            command.EntryId,
            nameof(command.EntryId),
            "Exercise entry id must not be empty.",
            value => new ExerciseEntryId(value));
        if (entryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(entryIdResult);
        }

        ExerciseEntryId entryId = entryIdResult.Value;
        ExerciseEntry? entry = await repository.GetByIdAsync(entryId, userIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (entry is null) {
            return Result.Failure(Errors.Exercise.NotAccessible(command.EntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
