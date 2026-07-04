using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Lessons.Common;

public interface INutritionLessonWriteRepository {
    Task<NutritionLesson?> GetByIdTrackingAsync(
        NutritionLessonId id,
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
