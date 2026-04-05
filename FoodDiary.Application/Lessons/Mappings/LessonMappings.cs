using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Mappings;

public static class LessonMappings {
    public static LessonSummaryModel ToSummaryModel(
        this NutritionLesson lesson,
        IReadOnlySet<NutritionLessonId> readLessonIds) {
        return new LessonSummaryModel(
            lesson.Id.Value,
            lesson.Title,
            lesson.Summary,
            lesson.Category.ToString(),
            lesson.Difficulty.ToString(),
            lesson.EstimatedReadMinutes,
            readLessonIds.Contains(lesson.Id));
    }

    public static LessonDetailModel ToDetailModel(this NutritionLesson lesson, bool isRead) {
        return new LessonDetailModel(
            lesson.Id.Value,
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Category.ToString(),
            lesson.Difficulty.ToString(),
            lesson.EstimatedReadMinutes,
            isRead);
    }
}
