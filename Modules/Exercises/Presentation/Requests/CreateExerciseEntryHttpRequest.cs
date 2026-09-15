namespace FoodDiary.Modules.Exercises.Presentation.Requests;

public sealed record CreateExerciseEntryHttpRequest(
    DateTime Date,
    string ExerciseType,
    int DurationMinutes,
    double CaloriesBurned,
    string? Name,
    string? Notes);
