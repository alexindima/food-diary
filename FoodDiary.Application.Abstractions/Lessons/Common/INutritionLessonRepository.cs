using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Lessons.Common;

public interface INutritionLessonRepository : INutritionLessonReadRepository, INutritionLessonReadModelRepository, INutritionLessonWriteRepository {
    new Task<IReadOnlyList<Guid>> GetReadLessonIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    new Task<bool> IsLessonReadAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default);
}
