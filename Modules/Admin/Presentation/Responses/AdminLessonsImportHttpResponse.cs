namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminLessonsImportHttpResponse(
    int ImportedCount,
    IReadOnlyList<AdminLessonHttpResponse> Lessons);
