using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Exercises.Models;

namespace FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;

public record CreateExerciseEntryCommand(
    Guid? UserId,
    DateTime Date,
    string ExerciseType,
    int DurationMinutes,
    double CaloriesBurned,
    string? Name,
    string? Notes) : ICommand<Result<ExerciseEntryModel>>, IUserRequest;
