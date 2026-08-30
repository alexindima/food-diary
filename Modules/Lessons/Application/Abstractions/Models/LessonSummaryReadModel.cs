namespace FoodDiary.Application.Abstractions.Lessons.Models;

public sealed record LessonSummaryReadModel(
    Guid Id,
    string Title,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder);
