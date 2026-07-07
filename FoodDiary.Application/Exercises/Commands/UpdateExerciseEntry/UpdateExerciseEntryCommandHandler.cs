using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;

public sealed class UpdateExerciseEntryCommandHandler(
    IExerciseEntryWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateExerciseEntryCommand, Result<ExerciseEntryModel>> {
    public async Task<Result<ExerciseEntryModel>> Handle(
        UpdateExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ExerciseEntryModel>(userIdResult);
        }

        Result<ExerciseEntryId> entryIdResult = RequiredIdParser.Parse(
            command.EntryId,
            nameof(command.EntryId),
            "Exercise entry id must not be empty.",
            value => new ExerciseEntryId(value));
        if (entryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ExerciseEntryModel, ExerciseEntryId>(entryIdResult);
        }

        ExerciseEntryId entryId = entryIdResult.Value;
        ExerciseEntry? entry = await repository.GetByIdAsync(entryId, userIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (entry is null) {
            return Result.Failure<ExerciseEntryModel>(Errors.Exercise.NotAccessible(command.EntryId));
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
