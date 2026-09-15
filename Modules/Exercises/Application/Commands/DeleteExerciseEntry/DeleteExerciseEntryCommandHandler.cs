using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Exercises.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Exercises.Application.Internal;
using FoodDiary.Modules.Exercises.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Exercises.Application.Commands.DeleteExerciseEntry;

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
            return Result.Failure(ExerciseErrors.NotAccessible(command.EntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
