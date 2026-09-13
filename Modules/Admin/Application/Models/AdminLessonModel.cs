namespace FoodDiary.Modules.Admin.Application.Models;

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
    DateTime? ModifiedOnUtc,
    bool IsPublished = true,
    int CompletedCount = 0);
