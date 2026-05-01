using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Lessons.Common;

public interface INutritionLessonRepository {
    Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NutritionLesson>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<NutritionLesson?> GetByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default);

    Task<NutritionLesson?> GetByIdTrackingAsync(
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

    Task AddAsync(
        NutritionLesson lesson,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<NutritionLesson> lessons,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        NutritionLesson lesson,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        NutritionLesson lesson,
        CancellationToken cancellationToken = default);
}
