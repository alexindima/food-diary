namespace FoodDiary.Presentation.Api.Features.Exercises.Requests;

public sealed record CreateExerciseEntryHttpRequest(
    DateTime Date,
    string ExerciseType,
    int DurationMinutes,
    double CaloriesBurned,
    string? Name,
    string? Notes);
