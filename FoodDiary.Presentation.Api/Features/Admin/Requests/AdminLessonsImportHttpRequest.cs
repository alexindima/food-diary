namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record AdminLessonsImportHttpRequest(
    int Version,
    IReadOnlyList<AdminLessonImportItemHttpRequest> Lessons);

public sealed record AdminLessonImportItemHttpRequest(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder);
