using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;

public class CreateExerciseEntryCommandHandler(IExerciseEntryWriteRepository repository)
    : ICommandHandler<CreateExerciseEntryCommand, Result<ExerciseEntryModel>> {
    public async Task<Result<ExerciseEntryModel>> Handle(
        CreateExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ExerciseEntryModel>(userIdResult.Error);
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
