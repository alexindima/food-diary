using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Lessons.Common;

public interface INutritionLessonReadModelRepository {
    Task<IReadOnlyList<LessonSummaryReadModel>> GetSummaryReadModelsByLocaleAsync(
        string locale,
        LessonCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<LessonSummaryPageReadModel> GetSummaryPageByLocaleAsync(
        string locale,
        LessonCategory? category,
        LessonDifficulty? difficulty,
        string? search,
        LessonSortOption sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountReadLessonsByLocaleAsync(
        UserId userId,
        string locale,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LessonAdminReadModel>> GetAdminReadModelsAsync(
        CancellationToken cancellationToken = default);

    Task<LessonDetailReadModel?> GetDetailReadModelByIdAsync(
        NutritionLessonId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetReadLessonIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsLessonReadAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken = default);
}
