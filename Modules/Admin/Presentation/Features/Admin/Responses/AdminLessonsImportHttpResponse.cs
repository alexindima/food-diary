namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminLessonsImportHttpResponse(
    int ImportedCount,
    IReadOnlyList<AdminLessonHttpResponse> Lessons);
