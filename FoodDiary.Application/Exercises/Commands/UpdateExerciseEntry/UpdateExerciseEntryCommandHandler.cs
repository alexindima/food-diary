using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;

public sealed class UpdateExerciseEntryCommandHandler(IExerciseEntryWriteRepository repository)
    : ICommandHandler<UpdateExerciseEntryCommand, Result<ExerciseEntryModel>> {
    public async Task<Result<ExerciseEntryModel>> Handle(
        UpdateExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<ExerciseEntryModel>(userIdResult);
        }

        var entryId = new ExerciseEntryId(command.EntryId);
        ExerciseEntry? entry = await repository.GetByIdAsync(entryId, userIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (entry is null) {
            return Result.Failure<ExerciseEntryModel>(Errors.Exercise.NotFound(command.EntryId));
        }

        ExerciseType? exerciseType = null;
        if (command.ExerciseType is not null && EnumValueParser.TryParse(command.ExerciseType, out ExerciseType parsed)) {
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

        await repository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success(entry.ToModel());
    }
}
