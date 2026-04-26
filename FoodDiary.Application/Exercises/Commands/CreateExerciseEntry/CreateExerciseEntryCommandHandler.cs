using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;

public class CreateExerciseEntryCommandHandler(IExerciseEntryRepository repository)
    : ICommandHandler<CreateExerciseEntryCommand, Result<ExerciseEntryModel>> {
    public async Task<Result<ExerciseEntryModel>> Handle(
        CreateExerciseEntryCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ExerciseEntryModel>(userIdResult.Error);
        }

        if (!Enum.TryParse<ExerciseType>(command.ExerciseType, true, out var exerciseType)) {
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

        await repository.AddAsync(entry, cancellationToken);
        return Result.Success(entry.ToModel());
    }
}
