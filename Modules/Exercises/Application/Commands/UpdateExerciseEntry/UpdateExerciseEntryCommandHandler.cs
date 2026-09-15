using FoodDiary.Modules.Exercises.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Exercises.Domain.Enums;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Exercises.Application.Internal;
using FoodDiary.Modules.Exercises.Application.Common;
using FoodDiary.Modules.Exercises.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Exercises.Application.Mappings;
using FoodDiary.Modules.Exercises.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Exercises.Application.Commands.UpdateExerciseEntry;

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

        Error? inputError = ExerciseEntryInputValidation.GetError(
            command.DurationMinutes,
            command.CaloriesBurned,
            command.Name,
            command.ClearName,
            command.Notes,
            command.ClearNotes);
        if (inputError is not null) {
            return Result.Failure<ExerciseEntryModel>(inputError);
        }

        ExerciseEntryId entryId = entryIdResult.Value;
        ExerciseEntry? entry = await repository.GetByIdAsync(entryId, userIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (entry is null) {
            return Result.Failure<ExerciseEntryModel>(ExerciseErrors.NotAccessible(command.EntryId));
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
