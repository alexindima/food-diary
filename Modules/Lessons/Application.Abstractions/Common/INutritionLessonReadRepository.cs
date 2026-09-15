using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Lessons.Application.Abstractions.Common;

public interface INutritionLessonReadRepository {
    Task<IReadOnlyList<NutritionLesson>> GetByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NutritionLesson>> GetAllAsync(
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
}
