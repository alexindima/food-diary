namespace FoodDiary.Modules.Lessons.Application.Abstractions.Models;

public sealed record LessonSummaryReadModel(
    Guid Id,
    string Title,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder);
