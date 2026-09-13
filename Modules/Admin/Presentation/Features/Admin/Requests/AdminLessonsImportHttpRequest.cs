namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record AdminLessonsImportHttpRequest(
    int Version,
    IReadOnlyList<AdminLessonImportItemHttpRequest> Lessons);
