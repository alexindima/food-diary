namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminLessonsImportHttpResponse(
    int ImportedCount,
    IReadOnlyList<AdminLessonHttpResponse> Lessons);
