namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminLessonUpdateHttpRequest(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder,
    bool IsPublished = true);
