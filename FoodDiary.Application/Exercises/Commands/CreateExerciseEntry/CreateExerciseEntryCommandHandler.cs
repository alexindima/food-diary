using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;

public sealed class CreateExerciseEntryCommandHandler(
    IExerciseEntryWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<CreateExerciseEntryCommand, Result<ExerciseEntryModel>> {
    public async Task<Result<ExerciseEntryModel>> Handle(
        CreateExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ExerciseEntryModel>(userIdResult);
        }

        if (!EnumValueParser.TryParse(command.ExerciseType, out ExerciseType exerciseType)) {
            exerciseType = ExerciseType.Other;
        }

        var entry = ExerciseEntry.Create(
            userIdResult.Value,
            command.Date,
            exerciseType,
            command.DurationMinutes,
            command.CaloriesBurned,
            command.Name,
            command.Notes);

        await repository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success(entry.ToModel());
    }
}
