using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Exercises.Contracts.Models;

namespace FoodDiary.Modules.Exercises.Application.Commands.CreateExerciseEntry;

public record CreateExerciseEntryCommand(
    Guid? UserId,
    DateTime Date,
    string ExerciseType,
    int DurationMinutes,
    double CaloriesBurned,
    string? Name,
    string? Notes) : ICommand<Result<ExerciseEntryModel>>, IUserRequest;
