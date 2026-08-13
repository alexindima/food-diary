namespace FoodDiary.Application.Admin.Models;

public sealed record AdminLessonModel(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);
