namespace FoodDiary.Modules.Lessons.Application.Models;

public sealed record LessonSummaryModel(
    Guid Id,
    string Title,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    bool IsRead);
