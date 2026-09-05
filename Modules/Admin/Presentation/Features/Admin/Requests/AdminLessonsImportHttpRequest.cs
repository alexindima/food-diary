namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record AdminLessonsImportHttpRequest(
    int Version,
    IReadOnlyList<AdminLessonImportItemHttpRequest> Lessons);
