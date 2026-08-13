namespace FoodDiary.Application.Exercises.Models;

public sealed record ExerciseEntryModel(
    Guid Id,
    DateTime Date,
    string ExerciseType,
    string? Name,
    int DurationMinutes,
    double CaloriesBurned,
    string? Notes);
