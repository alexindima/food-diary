namespace FoodDiary.Application.Abstractions.Lessons.Models;

public sealed record LessonDetailReadModel(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes);
