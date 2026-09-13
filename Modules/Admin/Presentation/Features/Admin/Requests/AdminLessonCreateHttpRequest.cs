namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record AdminLessonCreateHttpRequest(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder,
    bool IsPublished = true);
