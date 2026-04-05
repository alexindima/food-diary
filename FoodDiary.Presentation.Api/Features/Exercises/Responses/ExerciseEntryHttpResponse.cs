namespace FoodDiary.Presentation.Api.Features.Exercises.Responses;

public sealed record ExerciseEntryHttpResponse(
    Guid Id,
    DateTime Date,
    string ExerciseType,
    string? Name,
    int DurationMinutes,
    double CaloriesBurned,
    string? Notes);
