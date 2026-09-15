namespace FoodDiary.Modules.Lessons.Application.Abstractions.Models;

public sealed record LessonDetailReadModel(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes);
