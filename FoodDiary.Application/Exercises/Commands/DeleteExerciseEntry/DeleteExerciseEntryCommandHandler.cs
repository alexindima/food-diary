using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;

public class DeleteExerciseEntryCommandHandler(IExerciseEntryRepository repository)
    : ICommandHandler<DeleteExerciseEntryCommand, Result> {
    public async Task<Result> Handle(
        DeleteExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var entryId = new ExerciseEntryId(command.EntryId);
        var entry = await repository.GetByIdAsync(entryId, userIdResult.Value, asTracking: true, cancellationToken);
        if (entry is null) {
            return Result.Failure(Errors.Exercise.NotFound(command.EntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken);
        return Result.Success();
    }
}
