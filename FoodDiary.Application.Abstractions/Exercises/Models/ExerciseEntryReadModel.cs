namespace FoodDiary.Application.Abstractions.Exercises.Models;

public sealed record ExerciseEntryReadModel(
    Guid Id,
    DateTime Date,
    string ExerciseType,
    string? Name,
    int DurationMinutes,
    double CaloriesBurned,
    string? Notes);
