using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminLessonMappings {
    public static AdminLessonModel ToAdminModel(this LessonAdminReadModel lesson) =>
        new(
            lesson.Id,
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Locale,
            lesson.Category,
            lesson.Difficulty,
            lesson.EstimatedReadMinutes,
            lesson.SortOrder,
            lesson.CreatedOnUtc,
            lesson.ModifiedOnUtc, lesson.IsPublished, lesson.CompletedCount);
}
