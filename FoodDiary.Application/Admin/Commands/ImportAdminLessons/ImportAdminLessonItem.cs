namespace FoodDiary.Application.Admin.Commands.ImportAdminLessons;

public sealed record ImportAdminLessonItem(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder);
