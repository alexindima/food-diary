using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;

public class DeleteExerciseEntryCommandHandler(IExerciseEntryWriteRepository repository)
    : ICommandHandler<DeleteExerciseEntryCommand, Result> {
    public async Task<Result> Handle(
        DeleteExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var entryId = new ExerciseEntryId(command.EntryId);
        ExerciseEntry? entry = await repository.GetByIdAsync(entryId, userIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (entry is null) {
            return Result.Failure(Errors.Exercise.NotFound(command.EntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
