using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Common;

public interface INutritionLessonRepository {
    Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<NutritionLesson?> GetByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserLessonProgress>> GetUserProgressAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<UserLessonProgress?> GetUserProgressForLessonAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default);

    Task<UserLessonProgress> AddProgressAsync(
        UserLessonProgress progress,
        CancellationToken cancellationToken = default);
}
