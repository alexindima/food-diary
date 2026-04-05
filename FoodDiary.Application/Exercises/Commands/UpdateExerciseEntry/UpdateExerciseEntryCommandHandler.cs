using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;

public class UpdateExerciseEntryCommandHandler(IExerciseEntryRepository repository)
    : ICommandHandler<UpdateExerciseEntryCommand, Result<ExerciseEntryModel>> {
    public async Task<Result<ExerciseEntryModel>> Handle(
        UpdateExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ExerciseEntryModel>(userIdResult.Error);
        }

        var entryId = new ExerciseEntryId(command.EntryId);
        var entry = await repository.GetByIdAsync(entryId, userIdResult.Value, asTracking: true, cancellationToken);
        if (entry is null) {
            return Result.Failure<ExerciseEntryModel>(Errors.Exercise.NotFound(command.EntryId));
        }

        ExerciseType? exerciseType = null;
        if (command.ExerciseType is not null && Enum.TryParse<ExerciseType>(command.ExerciseType, true, out var parsed)) {
            exerciseType = parsed;
        }

        entry.Update(
            exerciseType,
            command.DurationMinutes,
            command.CaloriesBurned,
            command.Name,
            command.ClearName,
            command.Notes,
            command.ClearNotes,
            command.Date);

        await repository.UpdateAsync(entry, cancellationToken);
        return Result.Success(entry.ToModel());
    }
}
