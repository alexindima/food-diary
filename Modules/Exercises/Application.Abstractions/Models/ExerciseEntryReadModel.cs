namespace FoodDiary.Modules.Exercises.Application.Abstractions.Models;

public sealed record ExerciseEntryReadModel(
    Guid Id,
    DateTime Date,
    string ExerciseType,
    string? Name,
    int DurationMinutes,
    double CaloriesBurned,
    string? Notes);
