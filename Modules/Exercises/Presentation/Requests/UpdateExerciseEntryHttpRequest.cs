namespace FoodDiary.Modules.Exercises.Presentation.Requests;

public sealed record UpdateExerciseEntryHttpRequest(
    string? ExerciseType,
    int? DurationMinutes,
    double? CaloriesBurned,
    string? Name,
    bool ClearName,
    string? Notes,
    bool ClearNotes,
    DateTime? Date);
