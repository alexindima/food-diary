using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Lessons.Common;

public interface INutritionLessonReadModelRepository {
    Task<IReadOnlyList<LessonSummaryReadModel>> GetSummaryReadModelsByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LessonAdminReadModel>> GetAdminReadModelsAsync(
        CancellationToken cancellationToken = default);

    Task<LessonDetailReadModel?> GetDetailReadModelByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default);
}
