using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminLessonMappings {
    public static AdminLessonModel ToAdminModel(this NutritionLesson lesson) =>
        new(
            lesson.Id.Value,
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Locale,
            lesson.Category.ToString(),
            lesson.Difficulty.ToString(),
            lesson.EstimatedReadMinutes,
            lesson.SortOrder,
            lesson.CreatedOnUtc,
            lesson.ModifiedOnUtc);

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
            lesson.ModifiedOnUtc);
}
