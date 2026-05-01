namespace FoodDiary.Application.Admin.Models;

public sealed record AdminLessonsImportModel(
    int ImportedCount,
    IReadOnlyList<AdminLessonModel> Lessons);
