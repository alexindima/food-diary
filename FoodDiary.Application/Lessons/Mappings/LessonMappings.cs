using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Mappings;

public static class LessonMappings {
    extension(NutritionLesson lesson) {
        public LessonSummaryModel ToSummaryModel(IReadOnlySet<NutritionLessonId> readLessonIds) {
            return new LessonSummaryModel(
                lesson.Id.Value,
                lesson.Title,
                lesson.Summary,
                lesson.Category.ToString(),
                lesson.Difficulty.ToString(),
                lesson.EstimatedReadMinutes,
                readLessonIds.Contains(lesson.Id));
        }
        public LessonDetailModel ToDetailModel(bool isRead) {
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
}
