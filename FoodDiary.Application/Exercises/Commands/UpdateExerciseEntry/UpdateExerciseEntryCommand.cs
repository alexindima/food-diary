using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Exercises.Models;

namespace FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;

public record UpdateExerciseEntryCommand(
    Guid? UserId,
    Guid EntryId,
    string? ExerciseType,
    int? DurationMinutes,
    double? CaloriesBurned,
    string? Name,
    bool ClearName,
    string? Notes,
    bool ClearNotes,
    DateTime? Date) : ICommand<Result<ExerciseEntryModel>>, IUserRequest;
