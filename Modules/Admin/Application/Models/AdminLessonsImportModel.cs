namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminLessonsImportModel(
    int ImportedCount,
    IReadOnlyList<AdminLessonModel> Lessons);
