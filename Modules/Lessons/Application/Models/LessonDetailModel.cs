namespace FoodDiary.Application.Lessons.Models;

public sealed record LessonDetailModel(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    bool IsRead);
