namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminLessonsImportHttpRequest(
    int Version,
    IReadOnlyList<AdminLessonImportItemHttpRequest> Lessons);
